namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class MetricaSeparacionDTO
    {
        public string Nombre { get; set; } = "";
        public int CantidadVendida { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal Porcentaje { get; set; }
    }
}
