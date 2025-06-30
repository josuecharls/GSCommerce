namespace GSCommerce.Client.Models
{
    public class NotaCreditoDetalleDTO
    {
        public int Item { get; set; }
        public string IdArticulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "UND";
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal Total { get; set; }
    }
}
