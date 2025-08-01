namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteVentasVendedorDTO
    {
        public string NombreVendedor { get; set; } = "";
        public int TotalVentas { get; set; }
        public int TotalClientes { get; set; }
        public decimal MontoTotal { get; set; }
        public decimal TicketPromedio => TotalVentas > 0 ? Math.Round(MontoTotal / TotalVentas, 2) : 0;

    }
}
