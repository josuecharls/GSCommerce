namespace GSCommerceAPI.Models;

/// <summary>Meta diaria de ventas (monto) por almacén y fecha.</summary>
public partial class AlmacenMetaVentasDiaria
{
    public int IdAlmacen { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal MetaMontoDiario { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;
}
