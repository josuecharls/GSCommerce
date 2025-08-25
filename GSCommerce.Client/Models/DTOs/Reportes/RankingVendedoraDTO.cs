namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class RankingVendedoraDTO
    {
        public string Vendedora { get; set; } = string.Empty;
        public decimal TotalVentas { get; set; }
        public bool Activo { get; set; }
        public int TotalClientes { get; set; }
        public int VentasRealizadas { get; set; }
    }
}
