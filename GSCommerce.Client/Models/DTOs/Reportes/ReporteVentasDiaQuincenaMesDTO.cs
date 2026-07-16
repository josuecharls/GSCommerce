namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteVentasDiaQuincenaMesDTO
    {
        public int IdAlmacen { get; set; }
        public string Tienda { get; set; } = string.Empty;
        public decimal VentaDia { get; set; }
        public decimal VentaQuincena { get; set; }
        public decimal VentaMes { get; set; }
    }
}
