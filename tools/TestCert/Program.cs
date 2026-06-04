using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

var path = args.Length > 0 ? args[0] : "GSCommerceAPI/Certificados/certificadoBKD.p12";
var pass = args.Length > 1 ? args[1] : "e9gooyd84";

var certBytes = File.ReadAllBytes(path);
using var cert = new X509Certificate2(certBytes, pass,
    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);

Console.WriteLine($"HasPrivateKey: {cert.HasPrivateKey}");
var rsa = cert.GetRSAPrivateKey();
Console.WriteLine($"RSA KeySize: {rsa?.KeySize ?? 0}");

var xmlDoc = new XmlDocument { PreserveWhitespace = true };
xmlDoc.LoadXml("<root><ext:ExtensionContent xmlns:ext=\"urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2\"/></root>");

var signedXml = new SignedXml(xmlDoc) { SigningKey = rsa };
var reference = new Reference("") { DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1" };
reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
reference.AddTransform(new XmlDsigExcC14NTransform());
signedXml.AddReference(reference);
signedXml.KeyInfo = new KeyInfo();
signedXml.KeyInfo.AddClause(new KeyInfoX509Data(cert));
signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1";
signedXml.ComputeSignature();
Console.WriteLine("Firma XML (SUNAT): OK");
