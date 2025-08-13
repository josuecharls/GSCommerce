namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteTotalTiendasDTO
    {
        public int IdAlmacen { get; set; }
        public string Tienda { get; set; } = string.Empty;
        public decimal Venta { get; set; }
        public double Porcentaje { get; set; }
    }
}
