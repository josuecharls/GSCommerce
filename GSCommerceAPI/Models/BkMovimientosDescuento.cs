using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class BkMovimientosDescuento
{
    public int IdDescuento { get; set; }

    public int IdAlmacen { get; set; }

    public int IdArticulo { get; set; }

    public double Descuento { get; set; }
}
