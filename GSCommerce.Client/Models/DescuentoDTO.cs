namespace GSCommerce.Client.Models
{
    public class DescuentoDTO
    {
        public int IdDescuento { get; set; }
        public int IdAlmacen { get; set; }
        public int IdArticulo { get; set; }
        public string DescripcionCorta { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal PrecioCompra { get; set; }
        public double? PrecioFinal { get; set; }
        public double Descuento { get; set; }
        public double DescuentoPorc { get; set; }
        public double? Utilidad { get; set; }
    }
}