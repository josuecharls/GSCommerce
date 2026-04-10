namespace GSCommerceAPI.Models;

/// <summary>Meta diaria de cantidad de ventas (comprobantes) por almacén y fecha.</summary>
public partial class AlmacenMetaVentasDiaria
{
    public int IdAlmacen { get; set; }

    public DateOnly Fecha { get; set; }

    public int MetaVentasDiarias { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;
}
