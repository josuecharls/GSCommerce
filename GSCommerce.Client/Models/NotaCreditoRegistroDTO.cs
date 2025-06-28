namespace GSCommerce.Client.Models
{
    public class NotaCreditoRegistroDTO
    {
        public NotaCreditoCabeceraDTO Cabecera { get; set; } = new();
        public List<NotaCreditoDetalleDTO> Detalles { get; set; } = new();
        public int IdComprobanteOriginal { get; set; }
        public bool Manual { get; set; }
        public bool Interna { get; set; }
    }
}
