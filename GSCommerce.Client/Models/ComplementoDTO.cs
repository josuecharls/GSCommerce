namespace GSCommerce.Client.Models
{
    public class ComplementoDTO
    {
        public int IdComplemento { get; set; }
        public string Complemento { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
        public string? Alias { get; set; }
        public bool Estado { get; set; }
    }
}
