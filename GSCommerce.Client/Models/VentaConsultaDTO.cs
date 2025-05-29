namespace GSCommerce.Client.Models
{
    public class VentaConsultaDTO
    {
        public int IdComprobante { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}
