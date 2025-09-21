namespace GSCommerce.Client.Models.DTOs.Reportes;

public class ReporteGastosInversionDTO
{
    public int IdAlmacen { get; set; }
    public string Tienda { get; set; } = string.Empty;
    public decimal Alquiler { get; set; }
    public decimal Planillas { get; set; }
    public decimal Agua { get; set; }
    public decimal Luz { get; set; }
    public decimal Internet { get; set; }
    public decimal Sunat { get; set; }
    public decimal Diversos { get; set; }
    public decimal PlanillaServicios { get; set; }
    public decimal TotalInversion { get; set; }
    public decimal Venta { get; set; }
    public decimal CostoMercaderia { get; set; }
    public decimal UtilidadBruta { get; set; }
    public decimal UtilidadNeta { get; set; }
    public bool TieneAlquilerFijo { get; set; }
}

public class GuardarReporteGastosRequest
{
    public int IdAlmacen { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal Planillas { get; set; }
    public decimal Agua { get; set; }
    public decimal Luz { get; set; }
    public decimal Internet { get; set; }
    public decimal Sunat { get; set; }
    public decimal Diversos { get; set; }
    public decimal PlanillaServicios { get; set; }
}

public class GuardarReporteGastosAlquilerRequest
{
    public int IdAlmacen { get; set; }
    public decimal Alquiler { get; set; }
}