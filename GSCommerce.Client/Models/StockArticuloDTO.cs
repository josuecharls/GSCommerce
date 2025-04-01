namespace GSCommerce.Client.Models
{
    public class StockArticuloDTO
    {
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = string.Empty;
        public string Familia { get; set; } = string.Empty;
        public string Linea { get; set; } = string.Empty;
        public string IdArticulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal PrecioCompra { get; set; }
        public decimal? ValorCompra { get; set; }
        public double? PrecioVenta { get; set; }
        public double? ValorVenta { get; set; }
    }
}
