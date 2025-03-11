using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class StockAlmacen
{
    public int IdAlmacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public int Stock { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;
}
