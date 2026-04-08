namespace GSCommerce.Client.Models.DTOs.Reportes
{
    /// <summary>
    /// Marca para multiselección: <see cref="Clave"/> identifica variantes ("MVAR" / "MVAR."),
    /// <see cref="Etiqueta"/> es el texto mostrado.
    /// </summary>
    public class MarcaCatalogoDTO
    {
        public string Clave { get; set; } = "";
        public string Etiqueta { get; set; } = "";
    }
}
