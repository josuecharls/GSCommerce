using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VKardex2
{
    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public string Articulo { get; set; } = null!;

    public int SaldoInicial { get; set; }

    public decimal? ValorizadoInicial { get; set; }

    public int? Cantidad { get; set; }

    public decimal? Valorizado { get; set; }

    public int SaldoFinal { get; set; }

    public decimal? ValorizadoFinal { get; set; }
}
