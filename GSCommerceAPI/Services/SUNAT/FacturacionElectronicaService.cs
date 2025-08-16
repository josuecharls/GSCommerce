using GSCommerceAPI.Models.SUNAT.DTOs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.ServiceModel.Channels;
using System.ServiceModel;
using GSCommerceAPI.Models;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using ServicioSunat;

namespace GSCommerceAPI.Services.SUNAT
{
    public class FacturacionElectronicaService : IFacturacionElectronicaService
    {
        private readonly IWebHostEnvironment _env;
        private readonly SyscharlesContext _context;
        //_billServiceClient


        public FacturacionElectronicaService(IWebHostEnvironment env, SyscharlesContext context)
        {
            _env = env;
            _context = context;
        }

        public async Task<(bool exito, string mensaje)> EnviarResumenDiario(List<ComprobanteCabeceraDTO> comprobantes)
        {
            try
            {
                if (comprobantes == null || comprobantes.Count == 0)
                    return (false, "No hay comprobantes para enviar.");

                var empresa = comprobantes.First();

                // Validar certificado digital
                string nombreCertificado;
                try
                {
                    nombreCertificado = GetCertificadoArchivo(empresa.RucEmisor);
                }
                catch
                {
                    return (false, "Certificado digital no configurado para el emisor.");
                }

                string rutaCertificado = Path.Combine(_env.ContentRootPath, "Certificados", nombreCertificado);
                if (!File.Exists(rutaCertificado))
                    return (false, $"No se encontró el certificado {nombreCertificado}");

                // Obtener credenciales SOL
                var (usuarioSOL, claveSOL) = await ObtenerCredencialesSunatAsync(empresa.RucEmisor);
                if (string.IsNullOrWhiteSpace(usuarioSOL) || string.IsNullOrWhiteSpace(claveSOL))
                    return (false, "Credenciales SUNAT no configuradas.");

                // 1. Crear XML del resumen diario
                int correlativo = await _context.Resumen
                    .OrderByDescending(r => r.Correlativo)
                    .Select(r => (int?)r.Correlativo)
                    .FirstOrDefaultAsync() ?? 0;
                correlativo++;

                string nombreArchivoBase = $"{empresa.RucEmisor}-RC-{DateTime.Now:yyyyMMdd}-{correlativo:000}";
                string nombreArchivo = nombreArchivoBase + ".xml";
                string rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                var resumenXml = GenerarXmlResumen(comprobantes, empresa, correlativo);
                xmlDoc.LoadXml(resumenXml);
                xmlDoc.Save(rutaXml);

                // 2. Firmar XML
                string passwordCertificado = GetCertificadoPassword(empresa.RucEmisor);
                string hash = FirmarXml(resumenXml, rutaCertificado, passwordCertificado, rutaXml);

                // 3. Comprimir a ZIP
                var rutaZip = Path.ChangeExtension(rutaXml, ".zip");
                using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml));
                }

                // 4. Enviar a SUNAT
                var resultado = await EnviarResumenDiarioAsync(rutaZip, usuarioSOL, claveSOL);

                // 5. Guardar ticket en base de datos
                await GuardarResumenSunatAsync(comprobantes, nombreArchivo, hash, resultado.ticket, resultado.mensaje, resultado.exito, empresa.RazonSocialEmisor, correlativo);
                if (!resultado.exito)
                    return (false, resultado.mensaje);

                // 6. Consultar ticket y leer CDR
                await Task.Delay(TimeSpan.FromSeconds(5));
                await ValidarTicketSunatAsync(resultado.ticket, rutaZip, usuarioSOL, claveSOL);
                var (exitoSunat, codigoSunat, descripcionSunat) = await LeerCdrAsync(rutaZip);
                string rutaCdrXml = Path.Combine(Path.GetDirectoryName(rutaZip)!, Path.GetFileNameWithoutExtension(rutaZip) + ".xml");

                string cdrXml = string.Empty;
                int intentos = 0;
                while (!File.Exists(rutaCdrXml) && intentos < 5)
                {
                    await Task.Delay(1000); // espera 1 segundo
                    intentos++;
                }

                if (File.Exists(rutaCdrXml))
                {
                    cdrXml = await File.ReadAllTextAsync(rutaCdrXml);
                }

                await ActualizarResumenSunatAsync(nombreArchivo, $"SUNAT respondió: [{codigoSunat}] {descripcionSunat}", exitoSunat);

                foreach (var comp in comprobantes)
                {
                    await GuardarEstadoSunatAsync(
                        comp.IdComprobante,
                        hash,
                        cdrXml,
                        resultado.ticket,
                        $"SUNAT respondió: [{codigoSunat}] {descripcionSunat}",
                        exitoSunat,
                        false);
                }

                return (exitoSunat, $"Ticket: {resultado.ticket} | SUNAT: [{codigoSunat}] {descripcionSunat}");
            }
            catch (Exception ex)
            {
                return (false, $"Error en resumen diario: {ex.Message}");
            }
        }

        private async Task GuardarResumenSunatAsync(List<ComprobanteCabeceraDTO> comprobantes, string nombreArchivo, string hash, string ticket, string respuestaSunat, bool exito, string tienda, int correlativo)
        {
            var docInicio = $"{comprobantes.First().Serie}-{comprobantes.First().Numero:D8}";
            var docFin = $"{comprobantes.Last().Serie}-{comprobantes.Last().Numero:D8}";

            var resumen = new Resuman
            {
                Correlativo = correlativo,
                NombreArchivo = nombreArchivo,
                Hash = hash,
                DocInicio = docInicio,
                DocFin = docFin,
                CantidadDocumentos = comprobantes.Count,
                FechaEnvio = DateTime.Now,
                FechaReferencia = comprobantes.First().FechaEmision,
                EnvioSunat = exito,
                TicketSunat = ticket,
                RespuestaSunat = respuestaSunat,
                FechaRespuestaSunat = DateTime.Now,
                Tienda = tienda
            };

            _context.Resumen.Add(resumen);
            await _context.SaveChangesAsync();
        }

        private async Task ActualizarResumenSunatAsync(string nombreArchivo, string respuestaSunat, bool exito)
        {
            var resumen = await _context.Resumen.FirstOrDefaultAsync(r => r.NombreArchivo == nombreArchivo);
            if (resumen != null)
            {
                resumen.RespuestaSunat = respuestaSunat;
                resumen.FechaRespuestaSunat = DateTime.Now;
                resumen.EnvioSunat = exito;
                await _context.SaveChangesAsync();
            }
        }

        private async Task GuardarEstadoSunatAsync(int idComprobante, string hash, string xml, string ticket, string? respuestaSunat, bool exito, bool esNota)
        {
            var comprobante = await _context.Comprobantes
                .FirstOrDefaultAsync(c => c.IdComprobante == idComprobante);

            if (comprobante == null)
            {
                comprobante = new Comprobante
                {
                    IdComprobante = idComprobante
                };
                _context.Comprobantes.Add(comprobante);
            }

            comprobante.Hash = hash;
            comprobante.Xml = string.IsNullOrWhiteSpace(xml) ? null : xml;
            comprobante.EnviadoSunat = exito;
            comprobante.FechaEnvio = DateTime.Now;
            comprobante.TicketSunat = ticket;
            comprobante.RespuestaSunat = respuestaSunat;
            comprobante.FechaRespuestaSunat = DateTime.Now;
            comprobante.Estado = exito;
            comprobante.EsNota = false;
            comprobante.EsNota = esNota;

            Console.WriteLine($"[SUNAT] Guardado comprobante {idComprobante} - hash: {hash}, exito: {exito}");

            await _context.SaveChangesAsync();
        }

        private async Task<(string usuarioSOL, string claveSOL)> ObtenerCredencialesSunatAsync(string rucEmisor)
        {
            var credenciales = await _context.Almacens
                .Where(a => a.Ruc == rucEmisor)
                .Select(a => new { a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();

            if (credenciales == null)
            {
                Console.WriteLine($"[SUNAT] Error: No se encontraron credenciales para RUC {rucEmisor}");
                return (string.Empty, string.Empty);
            }

            var usuarioSOL = $"{rucEmisor}{credenciales.UsuarioSol}";
            Console.WriteLine($"[SUNAT] Credenciales obtenidas - RUC: {rucEmisor}, Usuario SOL: {usuarioSOL}, Clave SOL: {(string.IsNullOrEmpty(credenciales.ClaveSol) ? "VACÍA" : "******")}");
            return (usuarioSOL, credenciales.ClaveSol);
        }

        public async Task<(bool exito, string mensaje)> EnviarComprobante(ComprobanteCabeceraDTO comprobante)
        {
            try
            {
                comprobante.Serie = AsegurarSerieConPrefijo(comprobante.Serie, comprobante.TipoDocumento, comprobante.TipoDocumentoReferencia);
                var codigoTipoDoc = ObtenerCodigoTipoDocumentoSUNAT(comprobante.TipoDocumento);
                var nombreXml = $"{comprobante.RucEmisor}-{codigoTipoDoc}-{comprobante.Serie}-{comprobante.Numero:D8}.xml";
                var rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreXml);
                var rutaCertificado = Path.Combine(_env.ContentRootPath, "Certificados", GetCertificadoArchivo(comprobante.RucEmisor));
                Console.WriteLine($"[SUNAT] Firmando {comprobante.TipoDocumento}-{comprobante.Serie} con certificado {rutaCertificado}");

                var (generado, msg, hash) = await GenerarYFirmarFacturaAsync(comprobante, rutaXml);
                if (!generado) return (false, msg);

                var rutaZip = ComprimirArchivo(rutaXml);
                var (usuarioSOL, claveSOL) = await ObtenerCredencialesSunatAsync(comprobante.RucEmisor);

                var resultado = await EnviarFacturaAsync(rutaZip, usuarioSOL, claveSOL);

                string xmlFirmado = await File.ReadAllTextAsync(rutaXml);
                string carpeta = Path.GetDirectoryName(rutaZip)!;

                var rutaRespuestaXml = Path.Combine(carpeta, $"R-{comprobante.RucEmisor}-{codigoTipoDoc}-{comprobante.Serie}-{comprobante.Numero:D8}.xml");

                var (codigoRespuesta, descripcionRespuesta) = LeerRespuestaSunat(rutaRespuestaXml);
                bool exito = codigoRespuesta == "0";

                await GuardarEstadoSunatAsync(
                    comprobante.IdComprobante,
                    hash,
                    xmlFirmado,
                    "",
                    $"SUNAT respondió: [{codigoRespuesta}] {descripcionRespuesta}",
                    exito,
                    comprobante.TipoDocumento == "07" || comprobante.TipoDocumento == "08"
                );

                return (resultado.exito, resultado.mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error al enviar comprobante: {ex.Message}");
            }
        }

        public async Task<(bool exito, string mensaje, string hash)> GenerarYFirmarFacturaAsync(ComprobanteCabeceraDTO comprobante, string rutaXml)
        {
            try
            {
                comprobante.Serie = AsegurarSerieConPrefijo(comprobante.Serie, comprobante.TipoDocumento, comprobante.TipoDocumentoReferencia);
                // 1. Generar el XML como string (etiquetas UBL completas)
                var xml = GenerarXmlFactura(comprobante);
                // TEMPORAL: Guardar XML generado antes de firmar para revisión
                var rutaXmlTemporal = Path.Combine(_env.ContentRootPath, "Facturacion", $"temp_sin_firma_{Guid.NewGuid()}.xml"); await File.WriteAllTextAsync(rutaXmlTemporal, xml);
                Console.WriteLine("======= XML SIN FIRMA =======");
                Console.WriteLine(xml);
                Console.WriteLine("======= FIN XML SIN FIRMA =======");

                // 2. Ruta del certificado digital (Certificados/RUC.p12)
                var certificadoPath = Path.Combine(_env.ContentRootPath, "Certificados", GetCertificadoArchivo(comprobante.RucEmisor));
                var passwordCertificado = GetCertificadoPassword(comprobante.RucEmisor);
                var codigoTipoDoc = ObtenerCodigoTipoDocumentoSUNAT(comprobante.TipoDocumento);
                Console.WriteLine($"[SUNAT] RUC Emisor: {comprobante.RucEmisor}");
                Console.WriteLine($"[SUNAT] Serie: {comprobante.Serie}, Número: {comprobante.Numero:D8}");
                Console.WriteLine($"[SUNAT] Certificado: {certificadoPath}");
                Console.WriteLine($"[SUNAT] Password Certificado: {passwordCertificado}");
                // 3. Ruta de salida del archivo XML firmado
                var nombreArchivo = $"{comprobante.RucEmisor}-{codigoTipoDoc}-{comprobante.Serie}-{comprobante.Numero:D8}.xml";
                var rutaFinal = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                // 4. Firmar el XML y guardar
                var hash = FirmarXml(xml, certificadoPath, passwordCertificado, rutaFinal);

                return (true, "XML generado y firmado correctamente", hash);
            }
            catch (Exception ex)
            {
                return (false, $"Error al generar/firma XML: {ex.Message}", string.Empty);
            }
        }
        private string ObtenerCodigoTipoDocumentoSUNAT(string tipo)
        {
            tipo = tipo.ToUpper();
            return tipo switch
            {
                "01" or "FACTURA" or "FACTURA M" => "01",
                "03" or "BOLETA" or "BOLETA M" => "03",
                "07" or "NOTA CREDITO" => "07",
                "08" or "NOTA DEBITO" => "08",
                _ => throw new Exception($"Tipo de documento no válido: {tipo}")
            };
        }

        private string AsegurarSerieConPrefijo(string serie, string tipoDocumento, string? tipoDocumentoReferencia = null)
        {
            if (string.IsNullOrWhiteSpace(serie))
                return serie;

            if (char.IsLetter(serie[0]))
                return serie; // Ya tiene prefijo

            var tipoBase = tipoDocumento.ToUpper();
            if ((tipoBase == "07" || tipoBase == "08" || tipoBase == "NOTA CREDITO" || tipoBase == "NOTA DEBITO")
                && !string.IsNullOrWhiteSpace(tipoDocumentoReferencia))
            {
                tipoBase = tipoDocumentoReferencia.ToUpper();
            }

            var prefijo = tipoBase switch
            {
                "01" or "FACTURA" or "FACTURA M" => "F",
                "03" or "BOLETA" or "BOLETA M" => "B",
                _ => string.Empty
            };

            return string.Concat(prefijo, serie);
        }


        private string FirmarXml(string xmlContenido, string rutaCertificado, string password, string rutaSalida)
        {
            try
            {
                var xmlDoc = new XmlDocument { PreserveWhitespace = true };
                xmlDoc.LoadXml(xmlContenido);

                if (!File.Exists(rutaCertificado))
                    throw new FileNotFoundException($"No se encontró el certificado en {rutaCertificado}");

                var certBytes = File.ReadAllBytes(rutaCertificado);
                using var cert = new X509Certificate2(certBytes, password,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

                var signedXml = new SignedXml(xmlDoc)
                {
                    SigningKey = cert.GetRSAPrivateKey()
                };

                var reference = new Reference("")
                {
                    DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1"
                };
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                reference.AddTransform(new XmlDsigExcC14NTransform());
                signedXml.AddReference(reference);

                var keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));
                signedXml.KeyInfo = keyInfo;

                signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
                signedXml.ComputeSignature();

                var signature = signedXml.GetXml();
                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");
                var nodoExt = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsmgr);

                if (nodoExt != null)
                {
                    nodoExt.RemoveAll();
                    nodoExt.AppendChild(xmlDoc.ImportNode(signature, true));
                }
                else
                {
                    throw new InvalidOperationException("No se encontró el nodo ext:ExtensionContent para insertar la firma.");
                }

                xmlDoc.Save(rutaSalida);
                Console.WriteLine($"XML firmado guardado en: {rutaSalida}");

                return signedXml.GetXml().GetElementsByTagName("DigestValue")[0].InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al firmar XML: {ex.Message} - StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private string EscaparTextoXml(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return string.Empty;

            return texto.Replace("&", "&amp;")
                        .Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("'", "&apos;");
        }

        private string GenerarXmlFactura(ComprobanteCabeceraDTO dto)
        {
            string q = "\"";
            string doc = $"{dto.Serie}-{dto.Numero:D8}";
            var sb = new StringBuilder();

            // Determinar etiquetas según tipo de documento
            string rootTag = "Invoice";
            string rootNs = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
            string typeCodeTag = "InvoiceTypeCode";
            string lineTag = "InvoiceLine";
            string quantityTag = "InvoicedQuantity";
            if (dto.TipoDocumento == "07")
            {
                rootTag = "CreditNote";
                rootNs = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
                typeCodeTag = "CreditNoteTypeCode";
                lineTag = "CreditNoteLine";
                quantityTag = "CreditedQuantity";
            }
            else if (dto.TipoDocumento == "08")
            {
                rootTag = "DebitNote";
                rootNs = "urn:oasis:names:specification:ubl:schema:xsd:DebitNote-2";
                typeCodeTag = "DebitNoteTypeCode";
                lineTag = "DebitNoteLine";
                quantityTag = "DebitedQuantity";
            }
            // -- Manejo de descuentos globales --
            // Eliminamos las líneas de descuento y acumulamos su monto
            // para enviarlo como un descuento global (ID 2005).
            decimal descuentoBase = 0m;      // Monto sin IGV
            decimal descuentoIgv = 0m;       // IGV del descuento
            var detallesValidos = new List<ComprobanteDetalleDTO>();

            foreach (var det in dto.Detalles)
            {
                // Asegurar unidad de medida
                if (string.IsNullOrWhiteSpace(det.UnidadMedida))
                    det.UnidadMedida = "NIU";

                bool esDescuento =
                    det.PrecioUnitarioSinIGV < 0 ||
                    det.PrecioUnitarioConIGV < 0 ||
                    det.TotalSinIGV < 0 ||
                    det.Total < 0 ||

                    det.DescripcionItem.StartsWith("DESC", StringComparison.OrdinalIgnoreCase);

                if (esDescuento)
                {
                    descuentoBase += Math.Abs(det.TotalSinIGV);
                    descuentoIgv += Math.Abs(det.IGV);
                }
                else
                {
                    detallesValidos.Add(det);
                }
            }

            // Renumerar items para evitar saltos
            for (int i = 0; i < detallesValidos.Count; i++)
                detallesValidos[i].Item = i + 1;

            dto.Detalles = detallesValidos;

            descuentoBase = Math.Round(descuentoBase, 2);
            descuentoIgv = Math.Round(descuentoIgv, 2);
            decimal descuentoConIgv = Math.Round(descuentoBase + descuentoIgv, 2);

            // Recalcular totales netos a partir de los detalles válidos
            var subtotalPositivo = Math.Round(dto.Detalles.Sum(d => d.TotalSinIGV), 2);
            var igvPositivo = Math.Round(dto.Detalles.Sum(d => d.IGV), 2);

            dto.SubTotal = Math.Round(subtotalPositivo - descuentoBase, 2);
            dto.Igv = Math.Round(igvPositivo - descuentoIgv, 2);
            dto.Total = Math.Round(subtotalPositivo + igvPositivo - descuentoConIgv, 2);

            sb.AppendLine($"<?xml version={q}1.0{q} encoding={q}ISO-8859-1{q} standalone={q}no{q}?>");
            sb.AppendLine($"<{rootTag} xmlns={q}{rootNs}{q}" +
                          $" xmlns:cac={q}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{q}" +
                          $" xmlns:cbc={q}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{q}" +
                          $" xmlns:ds={q}http://www.w3.org/2000/09/xmldsig#{q}" +
                          $" xmlns:ext={q}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{q}" +
                          $" xmlns:sac={q}urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1{q}" +
                          $" xmlns:xsi={q}http://www.w3.org/2001/XMLSchema-instance{q}>");

            sb.AppendLine("<ext:UBLExtensions>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent><sac:AdditionalInformation>");
            sb.AppendLine("<sac:AdditionalMonetaryTotal><cbc:ID>1001</cbc:ID><cbc:PayableAmount currencyID=\"" + dto.Moneda + "\">" + dto.SubTotal.ToString("F2") + "</cbc:PayableAmount></sac:AdditionalMonetaryTotal>");
            if (descuentoConIgv > 0)
                sb.AppendLine("<sac:AdditionalMonetaryTotal><cbc:ID>2005</cbc:ID><cbc:PayableAmount currencyID=\"" + dto.Moneda + "\">-" + descuentoConIgv.ToString("F2") + "</cbc:PayableAmount></sac:AdditionalMonetaryTotal>"); sb.AppendLine("<sac:AdditionalProperty><cbc:ID>1000</cbc:ID><cbc:Value>" + EscaparTextoXml(dto.MontoLetras) + "</cbc:Value></sac:AdditionalProperty>");
            sb.AppendLine("</sac:AdditionalInformation></ext:ExtensionContent></ext:UBLExtension>");
            sb.AppendLine("</ext:UBLExtensions>");

            sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
            sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");
            sb.AppendLine("<cbc:ProfileID schemeURI=\"urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo17\" schemeAgencyName=\"PE: SUNAT\" schemeName=\"SUNAT: Identificador de Tipo de Operaci\u00f3n\">0101</cbc:ProfileID>");
            sb.AppendLine($"<cbc:ID>{doc}</cbc:ID>");
            sb.AppendLine($"<cbc:IssueDate>{dto.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
            sb.AppendLine($"<cbc:IssueTime>{dto.HoraEmision.ToString(@"hh\:mm\:ss")}</cbc:IssueTime>");
            if (dto.TipoDocumento == "07" || dto.TipoDocumento == "08")
                sb.AppendLine($"<cbc:{typeCodeTag}>{dto.TipoNotaCredito}</cbc:{typeCodeTag}>");
            else
                sb.AppendLine($"<cbc:{typeCodeTag} listID={q}0101{q}>{dto.TipoDocumento}</cbc:{typeCodeTag}>");
            sb.AppendLine($"<cbc:Note languageLocaleID={q}1000{q}><![CDATA[{dto.MontoLetras}]]></cbc:Note>");
            sb.AppendLine($"<cbc:DocumentCurrencyCode>{dto.Moneda}</cbc:DocumentCurrencyCode>");
            sb.AppendLine($"<cbc:LineCountNumeric>{dto.Detalles.Count}</cbc:LineCountNumeric>");
            if (dto.TipoDocumento == "07" || dto.TipoDocumento == "08")
            {
                string refId = $"{dto.SerieDocumentoReferencia}-{dto.NumeroDocumentoReferencia:D8}";
                sb.AppendLine("<cac:DiscrepancyResponse>");
                sb.AppendLine($"<cbc:ReferenceID>{refId}</cbc:ReferenceID>");
                sb.AppendLine($"<cbc:ResponseCode>{dto.TipoNotaCredito}</cbc:ResponseCode>");
                sb.AppendLine("</cac:DiscrepancyResponse>");
                sb.AppendLine("<cac:BillingReference>");
                sb.AppendLine("<cac:InvoiceDocumentReference>");
                sb.AppendLine($"<cbc:ID>{refId}</cbc:ID>");
                sb.AppendLine($"<cbc:DocumentTypeCode>{dto.TipoDocumentoReferencia}</cbc:DocumentTypeCode>");
                sb.AppendLine("</cac:InvoiceDocumentReference>");
                sb.AppendLine("</cac:BillingReference>");
            }
            //EMISOR
            sb.AppendLine("<cac:AccountingSupplierParty>");
            sb.AppendLine($"<cbc:CustomerAssignedAccountID schemeID={q}6{q}>{dto.RucEmisor}</cbc:CustomerAssignedAccountID>");
            sb.AppendLine($"<cbc:AdditionalAccountID>6</cbc:AdditionalAccountID>"); // Corregido: sin comillas
            sb.AppendLine("<cac:Party>");
            sb.AppendLine("<cac:PartyIdentification>"); // Nodo crítico que faltaba
            sb.AppendLine($"<cbc:ID schemeID={q}6{q}>{dto.RucEmisor}</cbc:ID>");
            sb.AppendLine("</cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyName>");
            sb.AppendLine($"<cbc:Name>{dto.RazonSocialEmisor}</cbc:Name>");
            sb.AppendLine("</cac:PartyName>");
            sb.AppendLine("<cac:PartyLegalEntity>");
            sb.AppendLine($"<cbc:RegistrationName>{dto.RazonSocialEmisor}</cbc:RegistrationName>");
            sb.AppendLine($"<cbc:CompanyID schemeID={q}6{q}>{dto.RucEmisor}</cbc:CompanyID>");
            sb.AppendLine("<cac:RegistrationAddress>");
            sb.AppendLine($"<cbc:ID>{dto.UbigeoEmisor}</cbc:ID>");
            sb.AppendLine("<cbc:AddressTypeCode>0000</cbc:AddressTypeCode>");
            sb.AppendLine("<cbc:CitySubdivisionName>NONE</cbc:CitySubdivisionName>");
            sb.AppendLine($"<cbc:CityName>{dto.ProvinciaEmisor}</cbc:CityName>");
            sb.AppendLine($"<cbc:CountrySubentity>{dto.DepartamentoEmisor}</cbc:CountrySubentity>");
            sb.AppendLine($"<cbc:District>{dto.DistritoEmisor}</cbc:District>");
            sb.AppendLine("<cac:AddressLine>");
            sb.AppendLine($"<cbc:Line>{dto.DireccionEmisor}</cbc:Line>");
            sb.AppendLine("</cac:AddressLine>");
            sb.AppendLine("<cac:Country>");
            sb.AppendLine("<cbc:IdentificationCode>PE</cbc:IdentificationCode>");
            sb.AppendLine("</cac:Country>");
            sb.AppendLine("</cac:RegistrationAddress>");
            sb.AppendLine("</cac:PartyLegalEntity>");
            sb.AppendLine("</cac:Party>");
            sb.AppendLine("</cac:AccountingSupplierParty>");
            //CLIENTE
            sb.AppendLine("<cac:AccountingCustomerParty>");
            sb.AppendLine($"<cbc:CustomerAssignedAccountID>{dto.DocumentoCliente}</cbc:CustomerAssignedAccountID>");
            sb.AppendLine($"<cbc:AdditionalAccountID>{dto.TipoDocumentoCliente}</cbc:AdditionalAccountID>");
            sb.AppendLine("<cac:Party>");
            sb.AppendLine("<cac:PartyIdentification>");

            string schemeIdCliente = dto.TipoDocumentoCliente switch
            {
                "0" => "0",  // Sin documento
                "1" => "1",  // DNI
                "6" => "6",  // RUC
                _ => "0"
            };
            sb.AppendLine($"<cbc:ID schemeID={q}{schemeIdCliente}{q}>{dto.DocumentoCliente}</cbc:ID>");
            sb.AppendLine("</cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyLegalEntity>");
            sb.AppendLine($"<cbc:RegistrationName>{dto.NombreCliente}</cbc:RegistrationName>");
            sb.AppendLine("</cac:PartyLegalEntity>");
            sb.AppendLine("</cac:Party>");
            sb.AppendLine("</cac:AccountingCustomerParty>");
            sb.AppendLine("<cac:PaymentTerms><cbc:ID>FormaPago</cbc:ID><cbc:PaymentMeansID>Contado</cbc:PaymentMeansID></cac:PaymentTerms>");
            // Totales globales
            sb.AppendLine("<cac:TaxTotal>");
            sb.AppendLine($"<cbc:TaxAmount currencyID=\"{dto.Moneda}\">{dto.Igv:F2}</cbc:TaxAmount>");
            sb.AppendLine("<cac:TaxSubtotal>");
            sb.AppendLine($"<cbc:TaxableAmount currencyID=\"{dto.Moneda}\">{dto.SubTotal:F2}</cbc:TaxableAmount>");
            sb.AppendLine($"<cbc:TaxAmount currencyID=\"{dto.Moneda}\">{dto.Igv:F2}</cbc:TaxAmount>");
            sb.AppendLine("<cac:TaxCategory><cac:TaxScheme><cbc:ID>1000</cbc:ID><cbc:Name>IGV</cbc:Name><cbc:TaxTypeCode>VAT</cbc:TaxTypeCode></cac:TaxScheme></cac:TaxCategory>");
            sb.AppendLine("</cac:TaxSubtotal>");
            sb.AppendLine("</cac:TaxTotal>");

            sb.AppendLine("<cac:LegalMonetaryTotal>");
            sb.AppendLine($"<cbc:LineExtensionAmount currencyID=\"{dto.Moneda}\">{dto.SubTotal:F2}</cbc:LineExtensionAmount>");
            sb.AppendLine($"<cbc:TaxInclusiveAmount currencyID=\"{dto.Moneda}\">{dto.Total:F2}</cbc:TaxInclusiveAmount>");
            sb.AppendLine($"<cbc:PayableAmount currencyID=\"{dto.Moneda}\">{dto.Total:F2}</cbc:PayableAmount>");
            sb.AppendLine("</cac:LegalMonetaryTotal>");

            foreach (var item in dto.Detalles)
            {
                var baseImponible = Math.Round(item.Cantidad * item.PrecioUnitarioSinIGV, 2);
                var igv = Math.Round(baseImponible * 0.18m, 2);

                sb.AppendLine($"<cac:{lineTag}>");
                sb.AppendLine($"<cbc:ID>{item.Item}</cbc:ID>");
                sb.AppendLine($"<cbc:{quantityTag} unitCode=\"{item.UnidadMedida}\">{item.Cantidad:F2}</cbc:{quantityTag}>");
                sb.AppendLine($"<cbc:LineExtensionAmount currencyID=\"{dto.Moneda}\">{baseImponible:F2}</cbc:LineExtensionAmount>");
                sb.AppendLine("<cac:PricingReference><cac:AlternativeConditionPrice>");
                sb.AppendLine($"<cbc:PriceAmount currencyID=\"{dto.Moneda}\">{item.PrecioUnitarioConIGV:F2}</cbc:PriceAmount>");
                sb.AppendLine("<cbc:PriceTypeCode>01</cbc:PriceTypeCode></cac:AlternativeConditionPrice></cac:PricingReference>");

                sb.AppendLine("<cac:TaxTotal><cbc:TaxAmount currencyID=\"" + dto.Moneda + "\">" + igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxSubtotal><cbc:TaxableAmount currencyID=\"" + dto.Moneda + "\">" + baseImponible.ToString("F2") + "</cbc:TaxableAmount>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"" + dto.Moneda + "\">" + igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxCategory>");
                sb.AppendLine("<cbc:Percent>18</cbc:Percent>");
                sb.AppendLine("<cbc:TaxExemptionReasonCode>10</cbc:TaxExemptionReasonCode>");
                sb.AppendLine("<cac:TaxScheme><cbc:ID>1000</cbc:ID><cbc:Name>IGV</cbc:Name><cbc:TaxTypeCode>VAT</cbc:TaxTypeCode></cac:TaxScheme>");
                sb.AppendLine("</cac:TaxCategory></cac:TaxSubtotal></cac:TaxTotal>");

                sb.AppendLine("<cac:Item><cbc:Description>" + EscaparTextoXml(item.DescripcionItem) + "</cbc:Description><cac:SellersItemIdentification><cbc:ID>" + item.CodigoItem + "</cbc:ID></cac:SellersItemIdentification></cac:Item>");
                sb.AppendLine("<cac:Price><cbc:PriceAmount currencyID=\"" + dto.Moneda + "\">" + item.PrecioUnitarioSinIGV.ToString("F2") + "</cbc:PriceAmount></cac:Price>");

                sb.AppendLine($"</cac:{lineTag}>");

            }
            //sb.AppendLine($"<cac:DeliveryLocation><cbc:ID>0000</cbc:ID></cac:DeliveryLocation>");
            sb.AppendLine($"</{rootTag}>");
            return sb.ToString();
        }

        private string GenerarXmlResumen(List<ComprobanteCabeceraDTO> comprobantes, ComprobanteCabeceraDTO empresa, int correlativoResumen)
        {
            var sb = new StringBuilder();
            string q = "\"";

            sb.AppendLine($"<?xml version={q}1.0{q} encoding={q}ISO-8859-1{q} standalone={q}no{q}?>");
            sb.AppendLine($"<SummaryDocuments xmlns={q}urn:oasis:names:specification:ubl:schema:xsd:SummaryDocuments-2{q} " +
                          $"xmlns:cac={q}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{q} " +
                          $"xmlns:cbc={q}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{q} " +
                          $"xmlns:ds={q}http://www.w3.org/2000/09/xmldsig#{q} " +
                          $"xmlns:ext={q}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{q} " +
                          $"xmlns:sac={q}urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1{q} " +
                          $"xmlns:xsi={q}http://www.w3.org/2001/XMLSchema-instance{q}>");

            sb.AppendLine("<ext:UBLExtensions>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
            sb.AppendLine("</ext:UBLExtensions>");

            sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
            sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");
            var fechaReferencia = comprobantes.First().FechaEmision;
            sb.AppendLine($"<cbc:ID>RC-{fechaReferencia:yyyyMMdd}-{correlativoResumen:000}</cbc:ID>");
            sb.AppendLine($"<cbc:ReferenceDate>{fechaReferencia:yyyy-MM-dd}</cbc:ReferenceDate>");
            sb.AppendLine($"<cbc:IssueDate>{DateTime.Now:yyyy-MM-dd}</cbc:IssueDate>");

            sb.AppendLine("<cac:Signature>");   
            sb.AppendLine($"<cbc:ID>IDSignKG</cbc:ID>");
            sb.AppendLine("<cac:SignatoryParty>");
            sb.AppendLine("<cac:PartyIdentification><cbc:ID>" + empresa.RucEmisor + "</cbc:ID></cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyName><cbc:Name>" + empresa.RazonSocialEmisor + "</cbc:Name></cac:PartyName>");
            sb.AppendLine("</cac:SignatoryParty>");
            sb.AppendLine("<cac:DigitalSignatureAttachment>");
            sb.AppendLine("<cac:ExternalReference><cbc:URI>#SignatureKG</cbc:URI></cac:ExternalReference>");
            sb.AppendLine("</cac:DigitalSignatureAttachment>");
            sb.AppendLine("</cac:Signature>");

            sb.AppendLine("<cac:AccountingSupplierParty>");
            sb.AppendLine("<cbc:CustomerAssignedAccountID>" + empresa.RucEmisor + "</cbc:CustomerAssignedAccountID>");
            sb.AppendLine("<cbc:AdditionalAccountID>6</cbc:AdditionalAccountID>");
            sb.AppendLine("<cac:Party>");
            sb.AppendLine("<cac:PartyLegalEntity>");
            sb.AppendLine("<cbc:RegistrationName>" + empresa.RazonSocialEmisor + "</cbc:RegistrationName>");
            sb.AppendLine("</cac:PartyLegalEntity>");
            sb.AppendLine("</cac:Party>");
            sb.AppendLine("</cac:AccountingSupplierParty>");

            int lineId = 1;
            foreach (var comp in comprobantes)
            {
                // Recalcular totales por comprobante para asegurar que
                // coincidan con la suma de sus detalles.
                comp.SubTotal = Math.Round(comp.Detalles.Sum(d => d.TotalSinIGV), 2);
                comp.Igv = Math.Round(comp.Detalles.Sum(d => d.IGV), 2);
                comp.Total = Math.Round(comp.SubTotal + comp.Igv, 2);

                sb.AppendLine("<sac:SummaryDocumentsLine>");
                var codigoTipoDoc = ObtenerCodigoTipoDocumentoSUNAT(comp.TipoDocumento);
                var serie = AsegurarSerieConPrefijo(comp.Serie, comp.TipoDocumento, comp.TipoDocumentoReferencia);
                sb.AppendLine($"<cbc:LineID>{lineId++}</cbc:LineID>");
                sb.AppendLine($"<cbc:DocumentTypeCode>{codigoTipoDoc}</cbc:DocumentTypeCode>");
                sb.AppendLine($"<cbc:ID>{serie}-{comp.Numero:D8}</cbc:ID>");
                sb.AppendLine("<cac:AccountingCustomerParty>");
                sb.AppendLine("<cbc:CustomerAssignedAccountID>" + comp.DocumentoCliente + "</cbc:CustomerAssignedAccountID>");
                sb.AppendLine("<cbc:AdditionalAccountID>" + comp.TipoDocumentoCliente + "</cbc:AdditionalAccountID>");
                sb.AppendLine("</cac:AccountingCustomerParty>");

                sb.AppendLine("<sac:Status><cbc:ConditionCode>1</cbc:ConditionCode></sac:Status>");

                sb.AppendLine("<sac:TotalAmount currencyID=\"" + comp.Moneda + "\">" + comp.Total.ToString("F2") + "</sac:TotalAmount>");

                sb.AppendLine("<sac:BillingPayment>");
                sb.AppendLine("<cbc:PaidAmount currencyID=\"" + comp.Moneda + "\">" + comp.SubTotal.ToString("F2") + "</cbc:PaidAmount>"); sb.AppendLine("<cbc:InstructionID>01</cbc:InstructionID>");
                sb.AppendLine("</sac:BillingPayment>");
                sb.AppendLine("<sac:BillingPayment>");
                sb.AppendLine("<cbc:PaidAmount currencyID=\"" + comp.Moneda + "\">" + comp.Igv.ToString("F2") + "</cbc:PaidAmount>");
                sb.AppendLine("<cbc:InstructionID>05</cbc:InstructionID>");
                sb.AppendLine("</sac:BillingPayment>");

                sb.AppendLine("<cac:AllowanceCharge>");
                sb.AppendLine("<cbc:ChargeIndicator>false</cbc:ChargeIndicator>");
                sb.AppendLine("<cbc:Amount currencyID=\"" + comp.Moneda + "\">0.00</cbc:Amount>");
                sb.AppendLine("</cac:AllowanceCharge>");

                sb.AppendLine("<cac:TaxTotal>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"" + comp.Moneda + "\">" + comp.Igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxSubtotal>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"" + comp.Moneda + "\">" + comp.Igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxCategory>");
                sb.AppendLine("<cac:TaxScheme><cbc:ID>1000</cbc:ID><cbc:Name>IGV</cbc:Name><cbc:TaxTypeCode>VAT</cbc:TaxTypeCode></cac:TaxScheme>");
                sb.AppendLine("</cac:TaxCategory>");
                sb.AppendLine("</cac:TaxSubtotal>");
                sb.AppendLine("</cac:TaxTotal>");

                sb.AppendLine("</sac:SummaryDocumentsLine>");
            }

            sb.AppendLine("</SummaryDocuments>");

            return sb.ToString();
        }

        public string ComprimirArchivo(string rutaXml)
        {
            var zipPath = rutaXml.Replace(".xml", ".zip");
            if (System.IO.File.Exists(zipPath))
                System.IO.File.Delete(zipPath);

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                archive.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml));
            }

            return zipPath;
        }

        public async Task<string> ValidarTicketSunatAsync(string ticket, string rutaArchivo, string usuarioSOL, string claveSOL)
        {
            try
            {
                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue,
                    Security =
            {
                Transport = { ClientCredentialType = HttpClientCredentialType.None },
                Message = { ClientCredentialType = BasicHttpMessageCredentialType.UserName }
            }
                };

                var direccion = new EndpointAddress("https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService");
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

                ws.Endpoint.EndpointBehaviors.Add(new SoapLogger
                {
                    LogFilePath = Path.Combine(_env.ContentRootPath, "soap_log_ticket.txt")
                });

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                var securityElement = elementos.Find<SecurityBindingElement>();
                if (securityElement != null)
                {
                    securityElement.EnableUnsecuredResponse = true;
                }
                ws.Endpoint.Binding = new CustomBinding(elementos);

                Console.WriteLine($"Consultando ticket: {ticket}");
                var respuesta = await ws.getStatusAsync(ticket);

                if (respuesta?.status?.content != null && respuesta.status.content.Length > 0)
                {
                    await File.WriteAllBytesAsync(rutaArchivo, respuesta.status.content);
                    var (exito, codigo, descripcion) = await LeerCdrAsync(rutaArchivo);
                    Console.WriteLine($"CDR recibido - Código: {codigo}, Descripción: {descripcion}");
                    return $"SUNAT respondió: [{codigo}] {descripcion}";
                }

                Console.WriteLine($"Sin CDR - Código de estado: {respuesta?.status?.statusCode}");
                return $"SUNAT no devolvió CDR. Código de estado: {respuesta?.status?.statusCode}";
            }
            catch (FaultException ex)
            {
                var detalle = ObtenerDetalleFaultException(ex);
                Console.WriteLine($"Error en consulta de ticket: {ex.Message} - Detalle: {detalle}");
                return $"SUNAT rechazó la consulta: {ex.Message} - Detalle: {detalle}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general en consulta: {ex.Message} - StackTrace: {ex.StackTrace}");
                return $"Error al consultar ticket: {ex.Message}";
            }
        }

        public async Task<(bool exito, string ticket, string mensaje)> EnviarResumenDiarioAsync(string rutaXmlZip, string usuarioSOL, string claveSOL)
        {
            try
            {
                byte[] archivoZip = await File.ReadAllBytesAsync(rutaXmlZip);
                string nombreZip = Path.GetFileName(rutaXmlZip);

                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue
                };
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                var direccion = new EndpointAddress("https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService");
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

                ws.Endpoint.EndpointBehaviors.Add(new SoapLogger
                {
                    LogFilePath = Path.Combine(_env.ContentRootPath, "soap_log.txt")
                });

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                elementos.Find<SecurityBindingElement>().EnableUnsecuredResponse = true;
                ws.Endpoint.Binding = new CustomBinding(elementos);

                var response = await ws.sendSummaryAsync(nombreZip, archivoZip, "1");

                return (true, response.ticket, "Ticket obtenido correctamente");
            }
            catch (FaultException ex)
            {
                var detalle = ObtenerDetalleFaultException(ex);
                var mensaje = $"SUNAT rechazó el envío: {ex.Message}";
                if (!string.IsNullOrWhiteSpace(detalle))
                {
                    mensaje += $" - {detalle}";
                }
                return (false, string.Empty, mensaje);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error general al enviar a SUNAT: {ex.Message}");
            }
        }




        public async Task<(bool exito, string ticket, string mensaje)> EnviarFacturaAsync(string rutaXmlZip, string usuarioSOL, string claveSOL)
        {
            try
            {
                // 1. Leer archivo ZIP
                byte[] archivoZip = await File.ReadAllBytesAsync(rutaXmlZip);
                string nombreZip = Path.GetFileName(rutaXmlZip);
                Console.WriteLine($"[SUNAT] Enviando archivo: {nombreZip}");

                var partes = Path.GetFileNameWithoutExtension(nombreZip).Split('-');
                if (partes.Length >= 4)
                {
                    Console.WriteLine($"[SUNAT] RUC: {partes[0]}, TipoDoc: {partes[1]}, Serie: {partes[2]}, Número: {partes[3]}");
                }
                Console.WriteLine($"[SUNAT] Usuario SOL: {usuarioSOL}");
                Console.WriteLine($"[SUNAT] Clave SOL: {claveSOL}");
                // 2. Configurar binding
                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue
                };
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                var direccion = new EndpointAddress("https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService");//producción SUNAT
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

                ws.Endpoint.EndpointBehaviors.Add(new SoapLogger
                {
                    LogFilePath = Path.Combine(_env.ContentRootPath, "soap_log.txt")
                });

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                // Habilitar respuestas no firmadas por SUNAT
                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                elementos.Find<SecurityBindingElement>().EnableUnsecuredResponse = true;
                ws.Endpoint.Binding = new CustomBinding(elementos);

                // 3. Enviar con sendBill
                var response = await ws.sendBillAsync(nombreZip, archivoZip, "0");
                byte[] contenido = response.applicationResponse;

                // 4. Guardar respuesta en disco
                string carpeta = Path.GetDirectoryName(rutaXmlZip)!;
                string nombreZipRespuesta = "R-" + nombreZip;
                string rutaZipRespuesta = Path.Combine(carpeta, nombreZipRespuesta);

                await File.WriteAllBytesAsync(rutaZipRespuesta, contenido);

                // 5. Extraer contenido (XML de SUNAT)
                if (File.Exists(rutaZipRespuesta))
                {
                    ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);

                    // ✅ Buscar XML extraído
                    string nombreXmlRespuesta = Path.GetFileNameWithoutExtension(nombreZipRespuesta).Replace("R-", "") + ".xml";
                    string rutaXmlRespuesta = Path.Combine(carpeta, nombreXmlRespuesta);

                    if (File.Exists(rutaXmlRespuesta))
                    {
                        var (codigo, descripcion) = LeerRespuestaSunat(rutaXmlRespuesta);
                        return (true, "", $"SUNAT respondió: [{codigo}] {descripcion}");
                    }

                    return (true, "", "Respuesta ZIP extraída pero no se encontró el XML de respuesta");
                }

                return (false, "", "SUNAT no devolvió ZIP de respuesta");
            }
            catch (FaultException ex)
            {
                var detalle = ObtenerDetalleFaultException(ex);
                var mensaje = $"SUNAT rechazó el envío: {ex.Message}";
                if (!string.IsNullOrWhiteSpace(detalle))
                {
                    mensaje += $" - {detalle}";
                }
                return (false, "", mensaje);
            }
            catch (Exception ex)
            {
                return (false, "", $"Error general al enviar a SUNAT: {ex.Message}");
            }
        }

        private string GetCertificadoArchivo(string rucEmisor)
        {
            return rucEmisor switch
            {
                "20611555203" => "certificadoCEMAVI.p12",
                "20613191624" => "certificadoFATISHOP.p12",
                "20611746751" => "certificadoLYMARIN.p12",
                "20606834048" => "certificadoROMAGE.p12",
                _ => throw new Exception("RUC no registrado para emisión electrónica")
            };
        }

        private string GetCertificadoPassword(string rucEmisor)
        {
            return rucEmisor switch
            {
                "20611555203" => "agglythes123",
                "20613191624" => "Fe101010",
                "20611746751" => "Marin123",
                "20606834048" => "Grupog2021",
                _ => throw new Exception("RUC no registrado para contraseña de certificado")
            };
        }

        private string ObtenerDetalleFaultException(FaultException ex)
        {
            try
            {
                var fault = ex.CreateMessageFault();
                if (fault.HasDetail)
                {
                    using var reader = fault.GetReaderAtDetailContents();
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    string detalle = doc.InnerText.Trim();
                    Console.WriteLine($"Detalle de fault: {detalle}");
                    return detalle;
                }
            }
            catch (Exception exInner)
            {
                Console.WriteLine($"Error al leer detalle de fault: {exInner.Message}");
            }
            return string.Empty;
        }

        private (string codigo, string descripcion) LeerRespuestaSunat(string rutaXmlRespuesta)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(rutaXmlRespuesta);

                // Namespaces necesarios
                var nsMgr = new XmlNamespaceManager(doc.NameTable);
                nsMgr.AddNamespace("ar", "urn:oasis:names:specification:ubl:schema:xsd:ApplicationResponse-2");
                nsMgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
                nsMgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

                // Buscar el nodo Response (3 métodos alternativos)
                var responseNode = doc.SelectSingleNode("//ar:ApplicationResponse/cac:DocumentResponse/cac:Response", nsMgr)
                                  ?? doc.SelectSingleNode("//cac:DocumentResponse/cac:Response", nsMgr)
                                  ?? doc.SelectSingleNode("//*[local-name()='Response']", nsMgr);
                //var responseNode = doc.SelectSingleNode("//*[namespace-uri()='urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2' and local-name()='Response']", nsMgr);

                if (responseNode == null)
                {
                    return ("-1", "Estructura de respuesta no reconocida");
                }

                var codigo = responseNode.SelectSingleNode("cbc:ResponseCode", nsMgr)?.InnerText ?? "Sin código";
                var descripcion = responseNode.SelectSingleNode("cbc:Description", nsMgr)?.InnerText ?? "Sin descripción";

                return (codigo, descripcion);
            }
            catch (Exception ex)
            {
                return ("-2", $"Error al leer XML: {ex.Message}");
            }
        }

        /* public async Task<(bool exito, string mensaje)> ConsultarTicketResumenAsync(string ticket, string rucEmisor)
         {
             try
             {
                 // 1. Enviar solicitud a SUNAT con el ticket
                 var respuesta = await _billServiceClient.getStatusAsync(ticket);

                 // 2. Leer y descomprimir el CDR desde los bytes que devuelve SUNAT
                 byte[] cdrZipBytes = respuesta.applicationResponse;
                 if (cdrZipBytes == null || cdrZipBytes.Length == 0)
                     return (false, "SUNAT no devolvió contenido en el CDR");

                 // 3. Extraer el XML del ZIP
                 string rutaCarpeta = Path.Combine(_env.ContentRootPath, "Facturacion", "CDR");
                 Directory.CreateDirectory(rutaCarpeta);
                 string rutaZip = Path.Combine(rutaCarpeta, $"CDR-{ticket}.zip");
                 await File.WriteAllBytesAsync(rutaZip, cdrZipBytes);

                 // Descomprimir ZIP y obtener archivo XML
                 string rutaXml = "";
                 using (ZipArchive zip = ZipFile.OpenRead(rutaZip))
                 {
                     foreach (var entry in zip.Entries)
                     {
                         if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                         {
                             rutaXml = Path.Combine(rutaCarpeta, entry.FullName);
                             entry.ExtractToFile(rutaXml, overwrite: true);
                             break;
                         }
                     }
                 }

                 if (string.IsNullOrEmpty(rutaXml))
                     return (false, "No se encontró XML en el ZIP del CDR");

                 // 4. Leer respuesta SUNAT desde el XML
                 var (codigo, descripcion) = LeerRespuestaSunat(rutaXml);
                 bool exito = codigo == "0";

                 // 5. Actualizar el estado del resumen en BD (opcional si deseas registrar en tabla Resumen)
                 // Aquí podrías actualizar FE.Resumen si tienes el ticket asociado
                 // await ActualizarResumenDesdeTicket(ticket, codigo, descripcion, exito); <-- opcional

                 return (exito, $"SUNAT respondió: [{codigo}] {descripcion}");
             }
             catch (Exception ex)
             {
                 return (false, $"Error al consultar ticket: {ex.Message}");
             }
         }*/

        public async Task<(bool exito, string codigoRespuesta, string mensaje)> LeerCdrAsync(string rutaZipRespuesta)
        {
            try
            {
                string carpeta = Path.GetDirectoryName(rutaZipRespuesta)!;

                // Extraer XML del ZIP de respuesta
                if (File.Exists(rutaZipRespuesta))
                {
                    ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);
                }

                // Buscar el XML dentro del ZIP
                string nombreXmlRespuesta = Path.GetFileNameWithoutExtension(rutaZipRespuesta) + ".xml";
                string rutaXmlRespuesta = Path.Combine(carpeta, nombreXmlRespuesta);

                if (!File.Exists(rutaXmlRespuesta))
                    return (false, "", "No se encontró el XML del CDR descomprimido");

                XmlDocument xml = new XmlDocument();
                xml.Load(rutaXmlRespuesta);


                // 1. Intenta buscar nodo DocumentResponse
                XmlNode? responseNode =
                    xml.SelectSingleNode("//*[local-name()='DocumentResponse']/*[local-name()='Response']");

                if (responseNode != null)
                {
                    string codigo = responseNode.SelectSingleNode("./*[local-name()='ResponseCode']")?.InnerText ?? "";
                    string descripcion = responseNode.SelectSingleNode("./*[local-name()='Description']")?.InnerText ?? "";

                    bool aceptado = codigo == "0";
                    return (aceptado, codigo, descripcion);
                }

                // 2. Si no hay DocumentResponse, buscar código de estado global (SUNAT aún lo procesa o error general)
                string responseCode = xml.SelectSingleNode("//*[local-name()='ResponseCode']")?.InnerText ?? "";
                string description = xml.SelectSingleNode("//*[local-name()='Description']")?.InnerText ?? "Sin descripción de SUNAT";

                if (!string.IsNullOrWhiteSpace(responseCode))
                {
                    return (false, responseCode, description);
                }

                return (false, "", "No se encontró el nodo de respuesta en el XML");
            }
            catch (Exception ex)
            {
                return (false, "", $"Error al leer CDR: {ex.Message}");
            }
        }

        private billServiceClient CrearClienteSunat(string usuarioSOL, string claveSOL)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
            {
                MaxReceivedMessageSize = int.MaxValue
            };
            binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

            var endpoint = new EndpointAddress("https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService");
            var client = new billServiceClient(binding, endpoint);

            client.ClientCredentials.UserName.UserName = usuarioSOL;
            client.ClientCredentials.UserName.Password = claveSOL;

            var elementos = client.Endpoint.Binding.CreateBindingElements();
            elementos.Find<SecurityBindingElement>().EnableUnsecuredResponse = true;
            client.Endpoint.Binding = new CustomBinding(elementos);

            return client;
        }

        /*
        private string GetCertificadoArchivo(string rucEmisor)
        {
            return $"{rucEmisor}.p12"; // ejemplo: 20552174918.p12
        }

        private string GetCertificadoPassword(string rucEmisor)
        {
            return "123456"; // puedes mejorar esto leyendo desde configuración o base de datos
        }*/
        // Implementaciones de EnviarResumenDiarioAsync, ComprimirArchivo, ValidarTicketSunatAsync irán después.
    }

}