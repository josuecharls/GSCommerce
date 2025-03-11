using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDetallesVenta
{
    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public int IdComprobante { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? UnidadMedida { get; set; }

    public int? Cantidad { get; set; }

    public decimal? Precio { get; set; }

    public decimal? PorcentajeDescuento { get; set; }

    public decimal? Total { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal? Costo { get; set; }

    public string? Ruc { get; set; }

    public string Proveedor { get; set; } = null!;
}
