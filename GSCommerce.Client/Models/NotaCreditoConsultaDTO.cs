namespace GSCommerce.Client.Models
{
    public class NotaCreditoConsultaDTO
    {
        public int IdNc { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Dniruc { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool? Empleada { get; set; }
        public string? Comprobante { get; set; }
    }
}