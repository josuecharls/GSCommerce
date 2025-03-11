using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class BkStocks25022020
{
    public int IdAlmacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public int Stock { get; set; }
}
