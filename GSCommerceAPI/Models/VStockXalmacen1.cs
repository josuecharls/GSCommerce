using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VStockXalmacen1
{
    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public string Familia { get; set; } = null!;

    public string Linea { get; set; } = null!;

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int Stock { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal? ValorCompra { get; set; }

    public double? PrecioVenta { get; set; }

    public double? ValorVenta { get; set; }

    public int? StockMinimo { get; set; }

    public bool EstaBajoMinimo { get; set; }
}
