﻿using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Descuento
{
    public int IdDescuento { get; set; }

    public int IdAlmacen { get; set; }

    public int IdArticulo { get; set; }

    public double Descuento1 { get; set; }
}
