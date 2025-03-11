using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VSerieCorrelativo1
{
    public int IdSerieCorrelativo { get; set; }

    public int IdAlmacen { get; set; }

    public int IdTipoDocumentoVenta { get; set; }

    public string DescripcionTipoDoc { get; set; } = null!;

    public string Serie { get; set; } = null!;

    public int Correlativo { get; set; }

    public bool Estado { get; set; }
}
