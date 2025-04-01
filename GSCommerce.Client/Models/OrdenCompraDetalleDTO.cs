namespace GSCommerce.Client.Models
{
    public class OrdenCompraDetalleDTO
    {
        public int IdOc { get; set; }
        public int Item { get; set; }
        public string IdArticulo { get; set; } = string.Empty;
        public string DescripcionArticulo { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Total { get; set; }
    }
}
