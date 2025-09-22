namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class LineaFiltroDTO
    {
        public string Linea { get; set; } = string.Empty;
        public string Familia { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;

        public string DisplayName => $"{Familia} - {Linea} - {Marca}";
    }
}