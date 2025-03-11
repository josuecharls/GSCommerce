using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VSeriesXcajero1
{
    public int IdSerieCorrelativo { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public string DocumentoSerie { get; set; } = null!;
}
