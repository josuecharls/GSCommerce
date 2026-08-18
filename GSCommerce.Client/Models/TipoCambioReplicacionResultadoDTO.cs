namespace GSCommerce.Client.Models
{
    public class TipoCambioReplicacionResultadoDTO
    {
        public string Accion { get; set; } = string.Empty;
        public DateOnly? FechaOrigen { get; set; }
        public DateOnly FechaDestino { get; set; }
        public decimal? Compra { get; set; }
        public decimal? Venta { get; set; }
    }
}
