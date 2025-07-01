namespace GSCommerceAPI.Models.DTOs
{
    public class NotaCreditoPDFDetalleDTO
    {
        public string Descripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Total { get; set; }
    }
}