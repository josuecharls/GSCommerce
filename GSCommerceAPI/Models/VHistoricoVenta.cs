using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VHistoricoVenta
{
    public int? Año { get; set; }

    public int? Mes { get; set; }

    public int? Dia { get; set; }

    public string? Fecha { get; set; }

    public string Almacen { get; set; } = null!;

    public decimal? Total { get; set; }
}
