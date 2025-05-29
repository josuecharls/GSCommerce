
    namespace GSCommerce.Client.Models
    {
    public class VentaDetalleDTO
    {
        public int Item { get; set; }
        public string CodigoItem { get; set; } = string.Empty;
        public string DescripcionItem { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "NIU";
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal PorcentajeDescuento { get; set; } // ejemplo: 0.05 = 5% de descuento
        public decimal Total { get; set; }
    }
}
