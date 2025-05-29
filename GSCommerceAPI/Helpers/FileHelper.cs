using System.Text;

namespace GSCommerceAPI.Helpers
{
    public static class FileHelper
    {
        public static void GuardarArchivo(string contenido, string rutaCompleta)
        {
            var directorio = Path.GetDirectoryName(rutaCompleta);
            if (!Directory.Exists(directorio))
                Directory.CreateDirectory(directorio!);

            File.WriteAllText(rutaCompleta, contenido, Encoding.GetEncoding("ISO-8859-1"));
        }
    }
}