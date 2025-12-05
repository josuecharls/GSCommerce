using GSCommerceAPI.Data;
using GSCommerceAPI.Models.SUNAT.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;
using ServicioSunat;

namespace GSCommerceAPI.Services.SUNAT
{
    public class GuiaRemisionElectronicaService : IGuiaRemisionElectronicaService
    {
        private readonly IWebHostEnvironment _env;
        private readonly SyscharlesContext _context;

        public GuiaRemisionElectronicaService(IWebHostEnvironment env, SyscharlesContext context)
        {
            _env = env;
            _context = context;
        }

        public async Task<(bool exito, string mensaje, string hash)> GenerarYFirmarGuiaAsync(GuiaRemisionDTO guia, string rutaXml)
        {
            try
            {
                // Validar serie
                if (!guia.Serie.StartsWith("T", StringComparison.OrdinalIgnoreCase) || guia.Serie.Length != 4)
                {
                    return (false, "La serie debe comenzar con 'T' y tener 4 caracteres", string.Empty);
                }

                // Generar XML
                var xml = GenerarXmlGuia(guia);
                
                // Ruta del certificado
                var certificadoPath = Path.Combine(_env.ContentRootPath, "Certificados", GetCertificadoArchivo(guia.RucRemitente));
                var passwordCertificado = GetCertificadoPassword(guia.RucRemitente);

                // Firmar XML
                var hash = FirmarXml(xml, certificadoPath, passwordCertificado, rutaXml);

                return (true, "XML generado y firmado correctamente", hash);
            }
            catch (Exception ex)
            {
                return (false, $"Error al generar/firmar XML: {ex.Message}", string.Empty);
            }
        }

        public async Task<(bool exito, string ticket, string mensaje)> EnviarGuiaAsync(string rutaXmlZip, string usuarioSOL, string claveSOL)
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

                // WSDL específico para guías de remisión
                var direccion = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itemision-guia-gem-beta/billService");
                var ws = new billServiceClient(binding, direccion);

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                var securityElement = elementos.Find<SecurityBindingElement>();
                if (securityElement != null)
                {
                    securityElement.EnableUnsecuredResponse = true;
                }
                ws.Endpoint.Binding = new CustomBinding(elementos);

                var response = await ws.sendBillAsync(nombreZip, archivoZip, "0");
                byte[] contenido = response.applicationResponse;

                // Guardar respuesta
                string carpeta = Path.GetDirectoryName(rutaXmlZip)!;
                string nombreZipRespuesta = "R-" + nombreZip;
                string rutaZipRespuesta = Path.Combine(carpeta, nombreZipRespuesta);

                await File.WriteAllBytesAsync(rutaZipRespuesta, contenido);

                // Extraer XML de respuesta
                if (File.Exists(rutaZipRespuesta))
                {
                    ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);
                    string nombreXmlRespuesta = Path.GetFileNameWithoutExtension(nombreZipRespuesta).Replace("R-", "") + ".xml";
                    string rutaXmlRespuesta = Path.Combine(carpeta, nombreXmlRespuesta);

                    if (File.Exists(rutaXmlRespuesta))
                    {
                        var (codigo, descripcion) = LeerRespuestaSunat(rutaXmlRespuesta);
                        bool aceptado = codigo == "0";
                        
                        // Intentar obtener IdGuiaRemision del nombre del archivo
                        // Formato: RUC-09-SERIE-NUMERO.xml
                        var partes = Path.GetFileNameWithoutExtension(nombreXmlRespuesta).Split('-');
                        if (partes.Length >= 4 && int.TryParse(partes[3], out int numero))
                        {
                            var guia = await _context.GuiaRemisionCabeceras
                                .FirstOrDefaultAsync(g => g.Serie == partes[2] && g.Numero == numero);
                            
                            if (guia != null)
                            {
                                await ActualizarEstadoSunatGuiaAsync(guia.IdGuiaRemision, codigo, descripcion, aceptado);
                            }
                        }
                        
                        return (true, "", $"SUNAT respondió: [{codigo}] {descripcion}");
                    }
                }

                return (true, "", "Guía enviada, pendiente de respuesta");
            }
            catch (FaultException ex)
            {
                return (false, "", $"SUNAT rechazó el envío: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, "", $"Error al enviar a SUNAT: {ex.Message}");
            }
        }

        public async Task<string> ValidarTicketGuiaAsync(string ticket, string rutaArchivo, string usuarioSOL, string claveSOL)
        {
            try
            {
                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential)
                {
                    MaxReceivedMessageSize = int.MaxValue
                };
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                var direccion = new EndpointAddress("https://e-beta.sunat.gob.pe/ol-ti-itemision-guia-gem-beta/billService");
                var ws = new billServiceClient(binding, direccion);

                ws.ClientCredentials.UserName.UserName = usuarioSOL;
                ws.ClientCredentials.UserName.Password = claveSOL;

                var elementos = ws.Endpoint.Binding.CreateBindingElements();
                var securityElement = elementos.Find<SecurityBindingElement>();
                if (securityElement != null)
                {
                    securityElement.EnableUnsecuredResponse = true;
                }
                ws.Endpoint.Binding = new CustomBinding(elementos);

                var respuesta = await ws.getStatusAsync(ticket);

                if (respuesta?.status?.content != null && respuesta.status.content.Length > 0)
                {
                    await File.WriteAllBytesAsync(rutaArchivo, respuesta.status.content);
                    var (exito, codigo, descripcion) = await LeerCdrGuiaAsync(rutaArchivo);
                    
                    // Intentar obtener IdGuiaRemision del nombre del archivo para actualizar estado
                    var nombreArchivoSinExt = Path.GetFileNameWithoutExtension(rutaArchivo);
                    if (nombreArchivoSinExt.StartsWith("R-"))
                    {
                        nombreArchivoSinExt = nombreArchivoSinExt.Substring(2);
                    }
                    var partes = nombreArchivoSinExt.Split('-');
                    if (partes.Length >= 4 && int.TryParse(partes[3], out int numero))
                    {
                        var guia = await _context.GuiaRemisionCabeceras
                            .FirstOrDefaultAsync(g => g.Serie == partes[2] && g.Numero == numero);
                        
                        if (guia != null)
                        {
                            await ActualizarEstadoSunatGuiaAsync(guia.IdGuiaRemision, codigo, descripcion, exito);
                        }
                    }
                    
                    return $"SUNAT respondió: [{codigo}] {descripcion}";
                }

                return $"SUNAT no devolvió CDR. Código de estado: {respuesta?.status?.statusCode}";
            }
            catch (Exception ex)
            {
                return $"Error al consultar ticket: {ex.Message}";
            }
        }

        public async Task<(bool exito, string codigoRespuesta, string mensaje)> LeerCdrGuiaAsync(string rutaZipRespuesta)
        {
            try
            {
                string carpeta = Path.GetDirectoryName(rutaZipRespuesta)!;

                if (File.Exists(rutaZipRespuesta))
                {
                    ZipFile.ExtractToDirectory(rutaZipRespuesta, carpeta, true);
                }

                string nombreXmlRespuesta = Path.GetFileNameWithoutExtension(rutaZipRespuesta) + ".xml";
                string rutaXmlRespuesta = Path.Combine(carpeta, nombreXmlRespuesta);

                if (!File.Exists(rutaXmlRespuesta))
                    return (false, "", "No se encontró el XML del CDR descomprimido");

                var xml = new XmlDocument();
                xml.Load(rutaXmlRespuesta);

                var responseNode = xml.SelectSingleNode("//*[local-name()='DocumentResponse']/*[local-name()='Response']");

                if (responseNode != null)
                {
                    string codigo = responseNode.SelectSingleNode("./*[local-name()='ResponseCode']")?.InnerText ?? "";
                    string descripcion = responseNode.SelectSingleNode("./*[local-name()='Description']")?.InnerText ?? "";
                    bool aceptado = codigo == "0";
                    return (aceptado, codigo, descripcion);
                }

                string responseCode = xml.SelectSingleNode("//*[local-name()='ResponseCode']")?.InnerText ?? "";
                string description = xml.SelectSingleNode("//*[local-name()='Description']")?.InnerText ?? "Sin descripción";

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

        public string ComprimirArchivo(string rutaXml)
        {
            var rutaZip = Path.ChangeExtension(rutaXml, ".zip");
            using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml));
            }
            return rutaZip;
        }

        public async Task<(bool exito, string mensaje)> EnviarGuiaCompletaAsync(GuiaRemisionDTO guia)
        {
            try
            {
                var nombreXml = $"{guia.RucRemitente}-09-{guia.Serie}-{guia.Numero:D8}.xml";
                var rutaXml = Path.Combine(_env.ContentRootPath, "Facturacion", nombreXml);

                var (generado, msg, hash) = await GenerarYFirmarGuiaAsync(guia, rutaXml);
                if (!generado) return (false, msg);

                // Leer XML firmado
                string xmlFirmado = await File.ReadAllTextAsync(rutaXml);

                var rutaZip = ComprimirArchivo(rutaXml);
                var (usuarioSOL, claveSOL) = await ObtenerCredencialesSunatAsync(guia.RucRemitente);

                var resultado = await EnviarGuiaAsync(rutaZip, usuarioSOL, claveSOL);

                // Guardar estado en BD
                await GuardarEstadoSunatGuiaAsync(guia.IdGuiaRemision, hash, xmlFirmado, resultado.ticket, resultado.mensaje, resultado.exito);

                return (resultado.exito, resultado.mensaje);
            }
            catch (Exception ex)
            {
                return (false, $"Error al enviar guía: {ex.Message}");
            }
        }

        private string GenerarXmlGuia(GuiaRemisionDTO guia)
        {
            string q = "\"";
            var sb = new StringBuilder();

            sb.AppendLine($"<?xml version={q}1.0{q} encoding={q}ISO-8859-1{q} standalone={q}no{q}?>");
            sb.AppendLine($"<DespatchAdvice xmlns={q}urn:oasis:names:specification:ubl:schema:xsd:DespatchAdvice-2{q}");
            sb.AppendLine($"    xmlns:ccts={q}urn:un:unece:uncefact:documentation:2{q}");
            sb.AppendLine($"    xmlns:cbc={q}urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2{q}");
            sb.AppendLine($"    xmlns:cac={q}urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2{q}");
            sb.AppendLine($"    xmlns:udt={q}urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2{q}");
            sb.AppendLine($"    xmlns:ext={q}urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2{q}");
            sb.AppendLine($"    xmlns:sac={q}urn:sunat:names:specification:ubl:peru:schema:xsd:SunatAggregateComponents-1{q}");
            sb.AppendLine($"    xmlns:qdt={q}urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2{q}");
            sb.AppendLine($"    xmlns:xsi={q}http://www.w3.org/2001/XMLSchema-instance{q}");
            sb.AppendLine($"    xmlns:ds={q}http://www.w3.org/2000/09/xmldsig#{q}>");

            // UBLExtensions
            sb.AppendLine("<ext:UBLExtensions>");
            sb.AppendLine("<ext:UBLExtension><ext:ExtensionContent/></ext:UBLExtension>");
            sb.AppendLine("</ext:UBLExtensions>");

            // Información básica
            sb.AppendLine("<cbc:UBLVersionID>2.1</cbc:UBLVersionID>");
            sb.AppendLine("<cbc:CustomizationID>2.0</cbc:CustomizationID>");
            sb.AppendLine($"<cbc:ID>{guia.Serie}-{guia.Numero:D8}</cbc:ID>");
            sb.AppendLine($"<cbc:IssueDate>{guia.FechaEmision:yyyy-MM-dd}</cbc:IssueDate>");
            sb.AppendLine($"<cbc:IssueTime>{guia.HoraEmision:hh\\:mm\\:ss}</cbc:IssueTime>");
            sb.AppendLine("<cbc:DespatchAdviceTypeCode>09</cbc:DespatchAdviceTypeCode>");

            if (!string.IsNullOrWhiteSpace(guia.Observaciones))
            {
                sb.AppendLine($"<cbc:Note>{EscaparTextoXml(guia.Observaciones)}</cbc:Note>");
            }

            // Remitente (SenderParty)
            sb.AppendLine("<cac:SenderParty>");
            sb.AppendLine("<cac:PartyIdentification>");
            sb.AppendLine($"<cbc:ID schemeID={q}6{q}>{guia.RucRemitente}</cbc:ID>");
            sb.AppendLine("</cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyLegalEntity>");
            sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(guia.RazonSocialRemitente)}</cbc:RegistrationName>");
            sb.AppendLine("</cac:PartyLegalEntity>");
            sb.AppendLine("<cac:PartyName>");
            sb.AppendLine($"<cbc:Name>{EscaparTextoXml(guia.RazonSocialRemitente)}</cbc:Name>");
            sb.AppendLine("</cac:PartyName>");
            sb.AppendLine("<cac:PostalAddress>");
            sb.AppendLine($"<cbc:ID>{guia.UbigeoRemitente}</cbc:ID>");
            sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(guia.DireccionRemitente)}</cbc:StreetName>");
            sb.AppendLine($"<cbc:CityName>{EscaparTextoXml(guia.DistritoRemitente)}</cbc:CityName>");
            sb.AppendLine($"<cbc:CountrySubentity>{EscaparTextoXml(guia.ProvinciaRemitente)}</cbc:CountrySubentity>");
            sb.AppendLine($"<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            sb.AppendLine("</cac:PostalAddress>");
            sb.AppendLine("</cac:SenderParty>");

            // Destinatario (ReceiverParty)
            sb.AppendLine("<cac:ReceiverParty>");
            sb.AppendLine("<cac:PartyIdentification>");
            sb.AppendLine($"<cbc:ID schemeID={q}{guia.TipoDocumentoDestinatario}{q}>{guia.NumeroDocumentoDestinatario}</cbc:ID>");
            sb.AppendLine("</cac:PartyIdentification>");
            sb.AppendLine("<cac:PartyLegalEntity>");
            sb.AppendLine($"<cbc:RegistrationName>{EscaparTextoXml(guia.RazonSocialDestinatario)}</cbc:RegistrationName>");
            sb.AppendLine("</cac:PartyLegalEntity>");
            sb.AppendLine("<cac:PostalAddress>");
            sb.AppendLine($"<cbc:ID>{guia.UbigeoDestinatario}</cbc:ID>");
            sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(guia.DireccionDestinatario)}</cbc:StreetName>");
            sb.AppendLine($"<cbc:CityName>{EscaparTextoXml(guia.DistritoDestinatario)}</cbc:CityName>");
            sb.AppendLine($"<cbc:CountrySubentity>{EscaparTextoXml(guia.ProvinciaDestinatario)}</cbc:CountrySubentity>");
            sb.AppendLine($"<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            sb.AppendLine("</cac:PostalAddress>");
            sb.AppendLine("</cac:ReceiverParty>");

            // Despatch (Origen)
            sb.AppendLine("<cac:Despatch>");
            sb.AppendLine("<cac:DespatchAddress>");
            sb.AppendLine($"<cbc:ID>{guia.UbigeoOrigen}</cbc:ID>");
            sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(guia.DireccionOrigen)}</cbc:StreetName>");
            sb.AppendLine($"<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            sb.AppendLine("</cac:DespatchAddress>");
            sb.AppendLine("</cac:Despatch>");

            // Delivery (Destino)
            sb.AppendLine("<cac:Delivery>");
            sb.AppendLine("<cac:DeliveryAddress>");
            sb.AppendLine($"<cbc:ID>{guia.UbigeoDestino}</cbc:ID>");
            sb.AppendLine($"<cbc:StreetName>{EscaparTextoXml(guia.DireccionDestino)}</cbc:StreetName>");
            sb.AppendLine($"<cac:Country><cbc:IdentificationCode>PE</cbc:IdentificationCode></cac:Country>");
            sb.AppendLine("</cac:DeliveryAddress>");
            sb.AppendLine("</cac:Delivery>");

            // Shipment (Transporte)
            if (guia.Transporte != null && guia.Transporte.EsTransportePrivado)
            {
                sb.AppendLine("<cac:Shipment>");
                sb.AppendLine("<cbc:ID>1</cbc:ID>");
                sb.AppendLine("<cbc:HandlingCode>01</cbc:HandlingCode>");
                
                if (guia.NumeroBultos.HasValue)
                {
                    sb.AppendLine($"<cbc:TotalTransportHandlingUnitQuantity>{guia.NumeroBultos.Value}</cbc:TotalTransportHandlingUnitQuantity>");
                }

                if (guia.PesoTotal.HasValue)
                {
                    sb.AppendLine("<cac:GrossWeightMeasure unitCode={q}KGM{q}>");
                    sb.AppendLine($"{guia.PesoTotal.Value:F2}");
                    sb.AppendLine("</cac:GrossWeightMeasure>");
                }

                // Transporte terrestre
                sb.AppendLine("<cac:TransportHandlingUnit>");
                sb.AppendLine("<cac:TransportEquipment>");
                if (!string.IsNullOrWhiteSpace(guia.Transporte.NumeroPlaca))
                {
                    sb.AppendLine($"<cbc:ID>{guia.Transporte.NumeroPlaca}</cbc:ID>");
                }
                if (!string.IsNullOrWhiteSpace(guia.Transporte.MarcaVehiculo))
                {
                    sb.AppendLine($"<cbc:TransportEquipmentTypeCode>{guia.Transporte.MarcaVehiculo}</cbc:TransportEquipmentTypeCode>");
                }
                sb.AppendLine("</cac:TransportEquipment>");
                sb.AppendLine("</cac:TransportHandlingUnit>");

                // Conductor
                if (!string.IsNullOrWhiteSpace(guia.Transporte.NumeroDocumentoConductor))
                {
                    sb.AppendLine("<cac:DriverPerson>");
                    sb.AppendLine($"<cbc:ID schemeID={q}{guia.Transporte.TipoDocumentoConductor}{q}>{guia.Transporte.NumeroDocumentoConductor}</cbc:ID>");
                    if (!string.IsNullOrWhiteSpace(guia.Transporte.NombreConductor))
                    {
                        sb.AppendLine($"<cbc:FirstName>{EscaparTextoXml(guia.Transporte.NombreConductor)}</cbc:FirstName>");
                    }
                    if (!string.IsNullOrWhiteSpace(guia.Transporte.NumeroLicencia))
                    {
                        sb.AppendLine($"<cbc:FamilyName>{EscaparTextoXml(guia.Transporte.NumeroLicencia)}</cbc:FamilyName>");
                    }
                    sb.AppendLine("</cac:DriverPerson>");
                }
                sb.AppendLine("</cac:Shipment>");
            }

            // DespatchLine (Detalles)
            foreach (var detalle in guia.Detalles)
            {
                sb.AppendLine("<cac:DespatchLine>");
                sb.AppendLine($"<cbc:ID>{detalle.Item}</cbc:ID>");
                sb.AppendLine($"<cbc:DeliveredQuantity unitCode={q}{detalle.UnidadMedida}{q}>{detalle.Cantidad:F2}</cbc:DeliveredQuantity>");
                
                if (detalle.Peso.HasValue)
                {
                    sb.AppendLine($"<cac:OrderLineReference><cac:OrderReference><cbc:ID>{detalle.Peso.Value:F2}</cbc:ID></cac:OrderReference></cac:OrderLineReference>");
                }

                sb.AppendLine("<cac:Item>");
                sb.AppendLine($"<cbc:Description>{EscaparTextoXml(detalle.DescripcionItem)}</cbc:Description>");
                sb.AppendLine("<cac:SellersItemIdentification>");
                sb.AppendLine($"<cbc:ID>{detalle.CodigoItem}</cbc:ID>");
                sb.AppendLine("</cac:SellersItemIdentification>");
                sb.AppendLine("</cac:Item>");
                sb.AppendLine("</cac:DespatchLine>");
            }

            // AdditionalInformation (Documentos de referencia)
            if (guia.DocumentosReferencia != null && guia.DocumentosReferencia.Any())
            {
                sb.AppendLine("<sac:AdditionalInformation>");
                foreach (var docRef in guia.DocumentosReferencia)
                {
                    sb.AppendLine("<sac:AdditionalMonetaryTotal>");
                    sb.AppendLine($"<cbc:ID>{docRef.TipoDocumento}</cbc:ID>");
                    sb.AppendLine($"<cbc:ReferenceAmount currencyID={q}PEN{q}>0.00</cbc:ReferenceAmount>");
                    sb.AppendLine("<sac:AdditionalProperty>");
                    sb.AppendLine($"<cbc:ID>{docRef.Serie}-{docRef.Numero:D8}</cbc:ID>");
                    if (docRef.FechaEmision.HasValue)
                    {
                        sb.AppendLine($"<cbc:Value>{docRef.FechaEmision.Value:yyyy-MM-dd}</cbc:Value>");
                    }
                    sb.AppendLine("</sac:AdditionalProperty>");
                    sb.AppendLine("</sac:AdditionalMonetaryTotal>");
                }
                sb.AppendLine("</sac:AdditionalInformation>");
            }

            sb.AppendLine("</DespatchAdvice>");

            return sb.ToString();
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
                return signedXml.GetXml().GetElementsByTagName("DigestValue")[0].InnerText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al firmar XML: {ex.Message}");
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
                        .Replace("'", "&apos;")
                        .Replace("\"", "&quot;");
        }

        private (string codigo, string descripcion) LeerRespuestaSunat(string rutaXmlRespuesta)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(rutaXmlRespuesta);

                var responseNode = doc.SelectSingleNode("//*[local-name()='DocumentResponse']/*[local-name()='Response']")
                                  ?? doc.SelectSingleNode("//*[local-name()='Response']");

                if (responseNode == null)
                {
                    return ("-1", "Estructura de respuesta no reconocida");
                }

                var codigo = responseNode.SelectSingleNode(".//*[local-name()='ResponseCode']")?.InnerText ?? "Sin código";
                var descripcion = responseNode.SelectSingleNode(".//*[local-name()='Description']")?.InnerText ?? "Sin descripción";

                return (codigo, descripcion);
            }
            catch (Exception ex)
            {
                return ("-2", $"Error al leer XML: {ex.Message}");
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

        private async Task<(string usuarioSOL, string claveSOL)> ObtenerCredencialesSunatAsync(string rucEmisor)
        {
            var credenciales = await _context.Almacens
                .Where(a => a.Ruc == rucEmisor)
                .Select(a => new { a.UsuarioSol, a.ClaveSol })
                .FirstOrDefaultAsync();

            if (credenciales == null)
            {
                return (string.Empty, string.Empty);
            }

            var usuarioSOL = $"{rucEmisor}{credenciales.UsuarioSol}";
            return (usuarioSOL, credenciales.ClaveSol);
        }

        private async Task GuardarEstadoSunatGuiaAsync(int idGuiaRemision, string hash, string xml, string ticket, string mensaje, bool exito)
        {
            try
            {
                var guiaSunat = await _context.GuiaRemisionSunats
                    .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

                if (guiaSunat == null)
                {
                    guiaSunat = new Models.GuiaRemisionSunat
                    {
                        IdGuiaRemision = idGuiaRemision,
                        FechaRegistro = DateTime.Now
                    };
                    _context.GuiaRemisionSunats.Add(guiaSunat);
                }

                guiaSunat.Hash = hash;
                guiaSunat.Xml = xml;
                guiaSunat.EnviadoSunat = exito;
                guiaSunat.FechaEnvio = DateTime.Now;
                guiaSunat.TicketSunat = ticket;
                guiaSunat.RespuestaSunat = mensaje;
                guiaSunat.EstadoSunat = exito ? "Enviado" : "Error";
                guiaSunat.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar estado SUNAT: {ex.Message}");
            }
        }

        public async Task ActualizarEstadoSunatGuiaAsync(int idGuiaRemision, string codigoRespuesta, string mensajeRespuesta, bool aceptado)
        {
            try
            {
                var guiaSunat = await _context.GuiaRemisionSunats
                    .FirstOrDefaultAsync(g => g.IdGuiaRemision == idGuiaRemision);

                if (guiaSunat != null)
                {
                    guiaSunat.CodigoRespuestaSunat = codigoRespuesta;
                    guiaSunat.MensajeRespuestaSunat = mensajeRespuesta;
                    guiaSunat.EstadoSunat = aceptado ? "Aceptado" : "Rechazado";
                    guiaSunat.FechaRespuestaSunat = DateTime.Now;
                    guiaSunat.FechaActualizacion = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar estado SUNAT: {ex.Message}");
            }
        }
    }
}
