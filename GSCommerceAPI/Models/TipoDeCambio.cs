using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class TipoDeCambio
{
    public DateOnly Fecha { get; set; }

    public decimal Compra { get; set; }

    public decimal Venta { get; set; }
}
