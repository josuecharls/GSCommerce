namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteAvanceHoraDTO
    {
        public DateTime HoraInicio { get; set; }
        public DateTime HoraFin { get; set; }
        public decimal TotalVentas { get; set; }
        public int Tickets { get; set; }
    }
}