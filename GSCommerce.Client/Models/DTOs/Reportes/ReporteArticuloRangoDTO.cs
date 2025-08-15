namespace GSCommerce.Client.Models.DTOs.Reportes;

public class ReporteArticuloRangoDTO
{
    public string IdArticulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public List<MesColDTO> Meses { get; set; } = new();
    public List<FilaAlmacenDTO> Filas { get; set; } = new();
    public TotalesFilaDTO Totales { get; set; } = new();
}

public class MesColDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = "";
    public string Key => $"{Year:D4}{Month:D2}";
}

public class FilaAlmacenDTO
{
    public string NombreAlmacen { get; set; } = "";
    public string Codigo { get; set; } = "";
    public int Ingreso { get; set; }
    public Dictionary<string, int> VentasPorMes { get; set; } = new();
    public int TotalVentas { get; set; }
    public DateTime? FechaPrimerIngreso { get; set; }
    public int Stock { get; set; }
    public decimal PorcentajeVendida { get; set; }
    public decimal PC { get; set; }
    public decimal PV { get; set; }
}

public class TotalesFilaDTO
{
    public int Ingreso { get; set; }
    public Dictionary<string, int> VentasPorMes { get; set; } = new();
    public int TotalVentas { get; set; }
    public int Stock { get; set; }
    public decimal PorcentajeVendida { get; set; }
}