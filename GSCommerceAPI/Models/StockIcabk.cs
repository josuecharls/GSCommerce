using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class StockIcabk
{
    public int IdAlmacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public int Stock { get; set; }
}
