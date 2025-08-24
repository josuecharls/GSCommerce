namespace GSCommerce.Client.Models
{
    public class AnulacionTicketDTO
    {
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public List<DetallePagoDTO> Pagos { get; set; } = new();
        public int IdUsuario { get; set; }
    }
}