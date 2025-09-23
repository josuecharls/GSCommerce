namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class TopArticuloDTO
    {
        public string Codigo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Linea { get; set; } = string.Empty;
        public string Familia { get; set; } = string.Empty;
        public int TotalUnidadesVendidas { get; set; }
        public decimal TotalImporte { get; set; }
    }
}