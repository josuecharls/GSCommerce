namespace GSCommerceAPI.Models.DTOs
{
    public class VentaResponseDTO
    {
        public int Numero { get; set; }
        public int IdComprobante { get; set; }
        public bool PendienteSunat { get; set; } = true;
    }
}