using System.IO.Compression;

namespace GSCommerceAPI.Helpers
{
    public static class ZipHelper
    {
        public static string ComprimirXml(string rutaXml)
        {
            string nombreZip = Path.GetFileNameWithoutExtension(rutaXml) + ".zip";
            string rutaZip = Path.Combine(Path.GetDirectoryName(rutaXml)!, nombreZip);

            if (File.Exists(rutaZip))
                File.Delete(rutaZip);

            using var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(rutaXml, Path.GetFileName(rutaXml), CompressionLevel.Fastest);

            return rutaZip;
        }
    }
}