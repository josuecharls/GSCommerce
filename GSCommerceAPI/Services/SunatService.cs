using System.IO.Compression;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Security;
using System.ServiceModel.Description;
using System.Text;
using System.Xml;

namespace GSCommerceAPI.Services
{
    public class SunatService
    {
        private const string UrlSunat = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService"; // Producción
        // private const string UrlSunat = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem/billService"; // Homologación

        public async Task<string> EnviarComprobanteASunat(string rutaZip, string nombreZip, string usuarioSol, string claveSol)
        {
            try
            {
                byte[] archivoZip = File.ReadAllBytes(rutaZip);

                var binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;
                binding.MaxReceivedMessageSize = 10000000; // Tamaño máximo

                var endpoint = new EndpointAddress(UrlSunat);
                var cliente = new ServicioSunat.billServiceClient(binding, endpoint);

                cliente.ClientCredentials.UserName.UserName = usuarioSol;
                cliente.ClientCredentials.UserName.Password = claveSol;

                var result = await cliente.sendBillAsync(nombreZip, archivoZip, "");

                if (result != null)
                {
                    string rutaRespuesta = rutaZip.Replace(".zip", "-R.xml");

                    using var stream = new MemoryStream(result.applicationResponse);
                    using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
                    zip.Entries.First().ExtractToFile(rutaRespuesta, true);

                    return $"✅ Enviado correctamente. CDR guardado: {rutaRespuesta}";
                }

                return "⚠️ No se recibió respuesta de SUNAT.";
            }
            catch (FaultException ex)
            {
                return $"❌ Error SUNAT: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"❌ Error general: {ex.Message}";
            }
        }

        public class RespuestaSunatDTO
        {
            public string Codigo { get; set; } = "";
            public string Mensaje { get; set; } = "";
            public string Estado { get; set; } = "";
        }

        public RespuestaSunatDTO LeerRespuestaSunat(string rutaXmlCdr)
        {
            var respuesta = new RespuestaSunatDTO();

            var xml = new XmlDocument();
            xml.Load(rutaXmlCdr);

            XmlNamespaceManager ns = new(xml.NameTable);
            ns.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");
            ns.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");

            var nodoResponseCode = xml.SelectSingleNode("//cac:DocumentResponse/cac:Response/cbc:ResponseCode", ns);
            var nodoDescription = xml.SelectSingleNode("//cac:DocumentResponse/cac:Response/cbc:Description", ns);

            respuesta.Codigo = nodoResponseCode?.InnerText ?? "";
            respuesta.Mensaje = nodoDescription?.InnerText ?? "";

            respuesta.Estado = respuesta.Codigo switch
            {
                "0" => "ACEPTADO",
                "98" => "EN PROCESO",
                "99" => "RECHAZADO",
                _ => "OBSERVADO"
            };

            return respuesta;
        }
    }
}
