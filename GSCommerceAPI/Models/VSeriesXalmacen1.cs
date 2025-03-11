using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VSeriesXalmacen1
{
    public int IdSerieCorrelativo { get; set; }

    public int IdAlmacen { get; set; }

    public string DocumentoSerie { get; set; } = null!;
}
