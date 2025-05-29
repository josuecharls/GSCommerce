namespace GSCommerce.Client.Models
{
    public class LiquidacionVentaDTO
    {
        public int IdAperturaCierre { get; set; }
        public decimal Total { get; set; }
        public List<ResumenCierreDeCajaDTO> Resumenes { get; set; } = new();
    }
}
