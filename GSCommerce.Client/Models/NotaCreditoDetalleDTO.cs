namespace GSCommerce.Client.Models
{
    public class NotaCreditoDetalleDTO
    {
        public int IdArticulo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "UND";
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal PorcentajeDescuento { get; set; }
        public decimal Total => Cantidad * Precio * (1 - PorcentajeDescuento);
    }
}
