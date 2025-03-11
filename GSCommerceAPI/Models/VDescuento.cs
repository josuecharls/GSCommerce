using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDescuento
{
    public int IdAlmacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string DescripcionCorta { get; set; } = null!;

    public decimal PrecioVenta { get; set; }

    public decimal PrecioCompra { get; set; }

    public double? PrecioFinal { get; set; }

    public double? Descuento { get; set; }

    public double DescuentoPorc { get; set; }

    public double? Utilidad { get; set; }
}
