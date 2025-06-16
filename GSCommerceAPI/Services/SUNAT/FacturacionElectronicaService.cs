using GSCommerceAPI.Models.SUNAT.DTOs;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
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

                // 1. Crear XML del resumen diario
                string nombreArchivoBase = $"{empresa.RucEmisor}-RC-{DateTime.Now:yyyyMMdd}-001";
                string nombreArchivo = nombreArchivoBase + ".xml";
                string rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreArchivo);

                var xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                var resumenXml = GenerarXmlResumen(comprobantes, empresa);
                xmlDoc.LoadXml(resumenXml);
                xmlDoc.Save(rutaXml);

                // 2. Firmar XML
                string rutaCertificado = Path.Combine(_env.ContentRootPath, "Certificados", GetCertificadoArchivo(empresa.RucEmisor));
                string passwordCertificado = GetCertificadoPassword(empresa.RucEmisor);

                FirmarXml(resumenXml, rutaCertificado, passwordCertificado, rutaXml);
                string hash = FirmarXml(resumenXml, rutaCertificado, passwordCertificado, rutaXml);

                // 3. Comprimir a ZIP
                var rutaZip = Path.ChangeExtension(rutaXml, ".zip");
                using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml));
                }

                // 4. Enviar a SUNAT
                string usuarioSOL = empresa.RucEmisor + "MODDATOS";
                string claveSOL = "MODDATOS";
                var resultado = await EnviarResumenDiarioAsync(rutaZip, usuarioSOL, claveSOL);

                // 5. Guardar en base de datos
                await GuardarResumenSunatAsync(comprobantes, nombreArchivo, hash, resultado.ticket, resultado.mensaje, resultado.exito, empresa.RazonSocialEmisor);

                if (!resultado.exito)
                    return (false, resultado.mensaje);

                return (true, $"Enviado correctamente. Ticket: {resultado.ticket} | SUNAT: {resultado.mensaje}");
            }
            catch (Exception ex)
            {
                return (false, $"Error en resumen diario: {ex.Message}");
            }
        }

        private async Task GuardarResumenSunatAsync(List<ComprobanteCabeceraDTO> comprobantes, string nombreArchivo, string hash, string ticket, string respuestaSunat, bool exito, string tienda)
        {
            var docInicio = $"{comprobantes.First().Serie}-{comprobantes.First().Numero:D8}";
            var docFin = $"{comprobantes.Last().Serie}-{comprobantes.Last().Numero:D8}";

            int correlativo = await _context.Resumen
                .OrderByDescending(r => r.Correlativo)
                .Select(r => (int?)r.Correlativo)
                .FirstOrDefaultAsync() ?? 0;

            var resumen = new Resuman
            {
                Correlativo = correlativo + 1,
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


        private async Task GuardarEstadoSunatAsync(int idComprobante, string hash, string xml, string ticket, string? respuestaSunat, bool exito)
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
            comprobante.Xml = xml;
            comprobante.EnviadoSunat = true;
            comprobante.FechaEnvio = DateTime.Now;
            comprobante.TicketSunat = ticket;
            comprobante.RespuestaSunat = respuestaSunat;
            comprobante.FechaRespuestaSunat = DateTime.Now;
            comprobante.Estado = exito;
            comprobante.EsNota = false;

            await _context.SaveChangesAsync();
        }

        public async Task<(bool exito, string mensaje)> EnviarComprobante(ComprobanteCabeceraDTO comprobante)
        {
            try
            {
                comprobante.Serie = AsegurarSerieConPrefijo(comprobante.Serie, comprobante.TipoDocumento);
                var codigoTipoDoc = ObtenerCodigoTipoDocumentoSUNAT(comprobante.TipoDocumento);
                var nombreXml = $"{comprobante.RucEmisor}-{codigoTipoDoc}-{comprobante.Serie}-{comprobante.Numero:D8}.xml";
                var rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreXml);

                var (generado, msg, hash) = await GenerarYFirmarFacturaAsync(comprobante, rutaXml);
                if (!generado) return (false, msg);

                var rutaZip = ComprimirArchivo(rutaXml);

                string usuarioSOL = $"{comprobante.RucEmisor}MODDATOS";
                string claveSOL = "MODDATOS";

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
                    exito
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
                comprobante.Serie = AsegurarSerieConPrefijo(comprobante.Serie, comprobante.TipoDocumento);
                // 1. Generar el XML como string (etiquetas UBL completas)
                var xml = GenerarXmlFactura(comprobante);
                // TEMPORAL: Guardar XML generado antes de firmar para revisión
                var rutaXmlTemporal = Path.Combine(_env.ContentRootPath, "Facturacion", "temp_sin_firma.xml");
                await File.WriteAllTextAsync(rutaXmlTemporal, xml);
                Console.WriteLine("======= XML SIN FIRMA =======");
                Console.WriteLine(xml);
                Console.WriteLine("======= FIN XML SIN FIRMA =======");

                // 2. Ruta del certificado digital (Certificados/RUC.p12)
                var certificadoPath = Path.Combine(_env.ContentRootPath, "Certificados", GetCertificadoArchivo(comprobante.RucEmisor));
                var passwordCertificado = GetCertificadoPassword(comprobante.RucEmisor);
                var codigoTipoDoc = ObtenerCodigoTipoDocumentoSUNAT(comprobante.TipoDocumento);
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

        private string AsegurarSerieConPrefijo(string serie, string tipoDocumento)
        {
            if (string.IsNullOrWhiteSpace(serie))
                return serie;

            if (char.IsLetter(serie[0]))
                return serie; // Ya tiene prefijo

            var prefijo = tipoDocumento.ToUpper() switch
            {
                "01" or "FACTURA" or "FACTURA M" => "F",
                "03" or "BOLETA" or "BOLETA M" => "B",
                _ => string.Empty
            };

            return string.Concat(prefijo, serie);
        }


        private string FirmarXml(string xmlContenido, string rutaCertificado, string password, string rutaSalida)
        {
            var xmlDoc = new XmlDocument { PreserveWhitespace = true };
            xmlDoc.LoadXml(xmlContenido);

            var cert = new X509Certificate2(rutaCertificado, password,
                      X509KeyStorageFlags.UserKeySet |
                      X509KeyStorageFlags.PersistKeySet |
                      X509KeyStorageFlags.Exportable);

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
            var nodoExt = xmlDoc.GetElementsByTagName("ext:ExtensionContent")[0];

            // Limpiar nodo existente antes de insertar
            if (nodoExt != null)
            {
                nodoExt.RemoveAll();
                nodoExt.AppendChild(xmlDoc.ImportNode(signature, true));
            }

            xmlDoc.Save(rutaSalida);

            // Retornar el digest value directamente (ya está en Base64)
            return signedXml.GetXml().GetElementsByTagName("DigestValue")[0].InnerText;
        }

        private string GenerarXmlFactura(ComprobanteCabeceraDTO dto)
        {
            string q = "\"";
            string doc = $"{dto.Serie}-{dto.Numero:D8}";
            var sb = new StringBuilder();

            sb.AppendLine($"<?xml version={q}1.0{q} encoding={q}ISO-8859-1{q} standalone={q}no{q}?>");
            sb.AppendLine($"<Invoice xmlns={q}urn:oasis:names:specification:ubl:schema:xsd:Invoice-2{q}" +
                          $" xmlns:cac={q}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{q}" +
                          $" xmlns:cbc={q}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{q}" +
                          $" xmlns:ds={q}http://www.w3.org/2000/09/xmldsig#{q}" +
                          $" xmlns:ext={q}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{q}" +
                          $" xmlns:sac={q}urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1{q}" +
                          $" xmlns:xsi={q}http://www.w3.org/2001/XMLSchema-instance{q}>");

            sb.AppendLine("<ext:UBLExtensions>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent><sac:AdditionalInformation>");
            sb.AppendLine("<sac:AdditionalMonetaryTotal><cbc:ID>1001</cbc:ID><cbc:PayableAmount currencyID=\"PEN\">" + dto.Total.ToString("F2") + "</cbc:PayableAmount></sac:AdditionalMonetaryTotal>");
            sb.AppendLine("<sac:AdditionalProperty><cbc:ID>1000</cbc:ID><cbc:Value>" + dto.MontoLetras + "</cbc:Value></sac:AdditionalProperty>");
            sb.AppendLine("</sac:AdditionalInformation></ext:ExtensionContent></ext:UBLExtension>");
            sb.AppendLine("</ext:UBLExtensions>");

            sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
            sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");
            sb.AppendLine("<cbc:ProfileID schemeURI=\"urn:pe:gob:sunat:cpe:see:gem:catalogos:catalogo17\" schemeAgencyName=\"PE: SUNAT\" schemeName=\"SUNAT: Identificador de Tipo de Operaci\u00f3n\">0101</cbc:ProfileID>");
            sb.AppendLine($"<cbc:ID>{doc}</cbc:ID>");
            sb.AppendLine($"<cbc:IssueDate>{dto.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
            sb.AppendLine($"<cbc:IssueTime>{dto.HoraEmision}</cbc:IssueTime>");
            sb.AppendLine($"<cbc:InvoiceTypeCode listID={q}0101{q}>{dto.TipoDocumento}</cbc:InvoiceTypeCode>");
            sb.AppendLine($"<cbc:Note languageLocaleID={q}1000{q}><![CDATA[{dto.MontoLetras}]]></cbc:Note>");
            sb.AppendLine($"<cbc:DocumentCurrencyCode>{dto.Moneda}</cbc:DocumentCurrencyCode>");
            sb.AppendLine($"<cbc:LineCountNumeric>{dto.Detalles.Count}</cbc:LineCountNumeric>");
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
            sb.AppendLine("<cbc:CityName>LIMA</cbc:CityName>");
            sb.AppendLine("<cbc:CountrySubentity>LIMA</cbc:CountrySubentity>");
            sb.AppendLine("<cbc:District>LIMA</cbc:District>");
            sb.AppendLine("<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
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
            sb.AppendLine($"<cbc:TaxAmount currencyID=\"PEN\">{dto.Igv:F2}</cbc:TaxAmount>");
            sb.AppendLine("<cac:TaxSubtotal>");
            sb.AppendLine($"<cbc:TaxableAmount currencyID=\"PEN\">{dto.SubTotal:F2}</cbc:TaxableAmount>");
            sb.AppendLine($"<cbc:TaxAmount currencyID=\"PEN\">{dto.Igv:F2}</cbc:TaxAmount>");
            sb.AppendLine("<cac:TaxCategory><cac:TaxScheme><cbc:ID>1000</cbc:ID><cbc:Name>IGV</cbc:Name><cbc:TaxTypeCode>VAT</cbc:TaxTypeCode></cac:TaxScheme></cac:TaxCategory>");
            sb.AppendLine("</cac:TaxSubtotal>");
            sb.AppendLine("</cac:TaxTotal>");

            sb.AppendLine("<cac:LegalMonetaryTotal>");
            sb.AppendLine($"<cbc:LineExtensionAmount currencyID=\"PEN\">{dto.SubTotal:F2}</cbc:LineExtensionAmount>");
            sb.AppendLine($"<cbc:TaxInclusiveAmount currencyID=\"PEN\">{dto.Total:F2}</cbc:TaxInclusiveAmount>");
            sb.AppendLine($"<cbc:PayableAmount currencyID=\"PEN\">{dto.Total:F2}</cbc:PayableAmount>");
            sb.AppendLine("</cac:LegalMonetaryTotal>");

            foreach (var item in dto.Detalles)
            {
                var baseImponible = Math.Round(item.Cantidad * item.PrecioUnitarioSinIGV, 2);
                var igv = Math.Round(baseImponible * 0.18m, 2);
                var total = baseImponible + igv;

                sb.AppendLine("<cac:InvoiceLine>");
                sb.AppendLine($"<cbc:ID>{item.Item}</cbc:ID>");
                sb.AppendLine($"<cbc:InvoicedQuantity unitCode=\"{item.UnidadMedida}\">{item.Cantidad:F2}</cbc:InvoicedQuantity>");
                sb.AppendLine($"<cbc:LineExtensionAmount currencyID=\"PEN\">{baseImponible:F2}</cbc:LineExtensionAmount>");

                sb.AppendLine("<cac:PricingReference><cac:AlternativeConditionPrice>");
                sb.AppendLine($"<cbc:PriceAmount currencyID=\"PEN\">{item.PrecioUnitarioConIGV:F2}</cbc:PriceAmount>");
                sb.AppendLine("<cbc:PriceTypeCode>01</cbc:PriceTypeCode></cac:AlternativeConditionPrice></cac:PricingReference>");

                sb.AppendLine("<cac:TaxTotal><cbc:TaxAmount currencyID=\"PEN\">" + igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxSubtotal><cbc:TaxableAmount currencyID=\"PEN\">" + baseImponible.ToString("F2") + "</cbc:TaxableAmount>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"PEN\">" + igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxCategory>");
                sb.AppendLine("<cbc:Percent>18</cbc:Percent>");
                sb.AppendLine("<cbc:TaxExemptionReasonCode>10</cbc:TaxExemptionReasonCode>");
                sb.AppendLine("<cac:TaxScheme><cbc:ID>1000</cbc:ID><cbc:Name>IGV</cbc:Name><cbc:TaxTypeCode>VAT</cbc:TaxTypeCode></cac:TaxScheme>");
                sb.AppendLine("</cac:TaxCategory></cac:TaxSubtotal></cac:TaxTotal>");

                sb.AppendLine("<cac:Item><cbc:Description>" + item.DescripcionItem + "</cbc:Description><cac:SellersItemIdentification><cbc:ID>" + item.CodigoItem + "</cbc:ID></cac:SellersItemIdentification></cac:Item>");
                sb.AppendLine("<cac:Price><cbc:PriceAmount currencyID=\"PEN\">" + item.PrecioUnitarioSinIGV.ToString("F2") + "</cbc:PriceAmount></cac:Price>");

                sb.AppendLine("</cac:InvoiceLine>");

            }
            //sb.AppendLine($"<cac:DeliveryLocation><cbc:ID>0000</cbc:ID></cac:DeliveryLocation>");
            sb.AppendLine("</Invoice>");
            return sb.ToString();
        }

        private string GenerarXmlResumen(List<ComprobanteCabeceraDTO> comprobantes, ComprobanteCabeceraDTO empresa)
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

            sb.AppendLine("<cbc:UBLVersionID>2.0</cbc:UBLVersionID>");
            sb.AppendLine("<cbc:CustomizationID>1.1</cbc:CustomizationID>");
            sb.AppendLine($"<cbc:ID>RC-{DateTime.Now:yyyyMMdd}-001</cbc:ID>");
            sb.AppendLine($"<cbc:ReferenceDate>{DateTime.Now:yyyy-MM-dd}</cbc:ReferenceDate>");
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

            int correlativo = 1;
            foreach (var comp in comprobantes)
            {
                sb.AppendLine("<sac:SummaryDocumentsLine>");
                sb.AppendLine($"<cbc:LineID>{correlativo++}</cbc:LineID>");
                sb.AppendLine("<cbc:DocumentTypeCode>03</cbc:DocumentTypeCode>");
                sb.AppendLine($"<cbc:ID>{comp.Serie}-{comp.Numero:D8}</cbc:ID>");
                sb.AppendLine("<cac:AccountingCustomerParty>");
                sb.AppendLine("<cbc:CustomerAssignedAccountID>" + comp.DocumentoCliente + "</cbc:CustomerAssignedAccountID>");
                sb.AppendLine("<cbc:AdditionalAccountID>" + comp.TipoDocumentoCliente + "</cbc:AdditionalAccountID>");
                sb.AppendLine("</cac:AccountingCustomerParty>");

                sb.AppendLine("<sac:Status><cbc:ConditionCode>1</cbc:ConditionCode></sac:Status>");

                sb.AppendLine("<sac:TotalAmount currencyID=\"PEN\">" + comp.Total.ToString("F2") + "</sac:TotalAmount>");

                sb.AppendLine("<sac:BillingPayment>");
                sb.AppendLine("<cbc:PaidAmount currencyID=\"PEN\">" + comp.Total.ToString("F2") + "</cbc:PaidAmount>");
                sb.AppendLine("<cbc:InstructionID>01</cbc:InstructionID>");
                sb.AppendLine("</sac:BillingPayment>");

                sb.AppendLine("<cac:TaxTotal>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"PEN\">" + comp.Igv.ToString("F2") + "</cbc:TaxAmount>");
                sb.AppendLine("<cac:TaxSubtotal>");
                sb.AppendLine("<cbc:TaxAmount currencyID=\"PEN\">" + comp.Igv.ToString("F2") + "</cbc:TaxAmount>");
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
            // TODO: Implementar validación real usando servicio SOAP SUNAT
            try
            {
                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue
                };
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                var direccion = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService");
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                elementos.Find<SecurityBindingElement>().EnableUnsecuredResponse = true;
                ws.Endpoint.Binding = new CustomBinding(elementos);

                var respuesta = await ws.getStatusAsync(ticket);

                if (respuesta?.status?.content != null && respuesta.status.content.Length > 0)
                {
                    await File.WriteAllBytesAsync(rutaArchivo, respuesta.status.content);
                    var (exito, codigo, descripcion) = await LeerCdrAsync(rutaArchivo);
                    return $"SUNAT respondió: [{codigo}] {descripcion}";
                }

                return $"SUNAT no devolvió CDR. Código de estado: {respuesta?.status?.statusCode}";
            }
            catch (FaultException ex)
            {
                return $"SUNAT rechazó la consulta: {ex.Message}";
            }
            catch (Exception ex)
            {
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

                var direccion = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService");
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                elementos.Find<SecurityBindingElement>().EnableUnsecuredResponse = true;
                ws.Endpoint.Binding = new CustomBinding(elementos);

                var response = await ws.sendSummaryAsync(nombreZip, archivoZip, "0");

                return (true, response.ticket, "Ticket obtenido correctamente");
            }
            catch (FaultException ex)
            {
                return (false, string.Empty, $"SUNAT rechazó el envío: {ex.Message}");
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

                // 2. Configurar binding
                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue
                };
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                var direccion = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService");//pruebas sunat
                var ws = new ServicioFacturacion2018.billServiceClient(binding, direccion);

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
                return (false, "", $"SUNAT rechazó el envío: {ex.Message}");
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

                XmlNamespaceManager nsm = new XmlNamespaceManager(xml.NameTable);
                nsm.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
                nsm.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

                // Ubicar el nodo con la respuesta
                XmlNode? responseNode = xml.SelectSingleNode("//cac:DocumentResponse/cac:Response", nsm);

                if (responseNode == null)
                    return (false, "", "No se encontró el nodo de respuesta en el XML");

                string codigo = responseNode.SelectSingleNode("cbc:ResponseCode", nsm)?.InnerText ?? "";
                string descripcion = responseNode.SelectSingleNode("cbc:Description", nsm)?.InnerText ?? "";

                bool aceptado = codigo == "0";

                return (aceptado, codigo, descripcion);
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

            var endpoint = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService");
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