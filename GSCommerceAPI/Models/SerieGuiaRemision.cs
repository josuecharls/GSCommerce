using System;

namespace GSCommerceAPI.Models;

public partial class SerieGuiaRemision
{
    public int IdSerieGuiaRemision { get; set; }
    public int IdAlmacen { get; set; }
    public string Serie { get; set; } = null!;
    public int Correlativo { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    
    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;
}
