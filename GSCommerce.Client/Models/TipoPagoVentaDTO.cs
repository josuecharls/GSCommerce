namespace GSCommerce.Client.Models
{
    public class TipoPagoVentaDTO
    {
        public int IdTipoPagoVenta { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}