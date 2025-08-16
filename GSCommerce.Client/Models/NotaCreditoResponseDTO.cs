namespace GSCommerce.Client.Models
{
    public class NotaCreditoResponseDTO
    {
        public int Numero { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int IdNc { get; set; }
        public string? Error { get; set; }
    }
}