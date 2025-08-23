namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteUtilidadTiendasDTO
    {
        public int IdAlmacen { get; set; }
        public string Tienda { get; set; } = string.Empty;
        public decimal Utilidad { get; set; }
        public double PorcentajeUtilidad { get; set; }
        public decimal Venta { get; set; }
        public double PorcentajeVenta { get; set; }
    }
}
