namespace GSCommerce.Client.Models
{
    public class VentasDiariasCajaResponseDTO
    {
        public List<VCierreVentaDiaria1DTO> Ventas { get; set; } = new();
        public decimal TotalEfectivo { get; set; }
        public decimal TotalTarjeta { get; set; }
        public decimal TotalNc { get; set; }
        public decimal TotalVentas { get; set; }
    }
}
