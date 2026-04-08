namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class VentaTarjetaMensualDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string AlmacenNombre { get; set; } = string.Empty;
        public decimal TotalSoles { get; set; }
    }

    public class ReporteVentasTarjetaMensualResponseDTO
    {
        public List<VentaTarjetaMensualDTO> Resultados { get; set; } = new();
    }
}
