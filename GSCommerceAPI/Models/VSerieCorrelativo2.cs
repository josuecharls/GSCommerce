using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VSerieCorrelativo2
{
    public int IdAsignacion { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public int IdTipoDocumentoVenta { get; set; }

    public int IdSerieCorrelativo { get; set; }

    public string Serie { get; set; } = null!;

    public int Correlativo { get; set; }

    public bool Estado { get; set; }
}
