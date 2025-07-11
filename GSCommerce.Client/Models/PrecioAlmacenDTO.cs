namespace GSCommerce.Client.Models
{
    public class PrecioAlmacenDTO
    {
        public int IdAlmacen { get; set; }
        public string IdArticulo { get; set; } = string.Empty;
        public string DescripcionCorta { get; set; } = string.Empty;
        public decimal PrecioVenta { get; set; }
        public decimal PrecioCompra { get; set; }
        public double? PrecioFinal { get; set; }
        public double? Descuento { get; set; }
        public double DescuentoPorc { get; set; }
    }
}