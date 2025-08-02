using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace GSCommerceAPI.Services
{
    public class XmlSignerService
    {
        private readonly string _certPath;
        private readonly string _certPassword;

        public XmlSignerService(string certPath, string certPassword)
        {
            _certPath = certPath;
            _certPassword = certPassword;
        }

        public (string signedXml, string hash) FirmarXML(string xmlContent, string firmaId = "SignatureKG")
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            xmlDoc.LoadXml(xmlContent);

            if (!File.Exists(_certPath))
                throw new FileNotFoundException($"No se encontró el certificado en {_certPath}");

            var certBytes = File.ReadAllBytes(_certPath);
            using var certificado = new X509Certificate2(certBytes, _certPassword,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

            var signedXml = new SignedXml(xmlDoc)
            {
                SigningKey = certificado.GetRSAPrivateKey()
            };

            var reference = new Reference(string.Empty);
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificado));
            signedXml.KeyInfo = keyInfo;
            signedXml.Signature.Id = firmaId;

            signedXml.ComputeSignature();

            // 🧬 Obtenemos el hash (DigestValue)
            var digest = reference.DigestValue;
            string hash = Convert.ToBase64String(digest);

            // Buscamos <ext:ExtensionContent> vacío
            var nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2");

            var nodoExtension = xmlDoc.SelectSingleNode("//ext:ExtensionContent", nsMgr);
            if (nodoExtension == null)
                throw new Exception("No se encontró el nodo ExtensionContent");

            nodoExtension.AppendChild(xmlDoc.ImportNode(signedXml.GetXml(), true));

            // Devolver como string
            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Encoding = Encoding.GetEncoding("ISO-8859-1"), Indent = true });
            xmlDoc.WriteTo(xmlWriter);
            xmlWriter.Flush();

            return (stringWriter.ToString(), hash);
        }
    }
}
