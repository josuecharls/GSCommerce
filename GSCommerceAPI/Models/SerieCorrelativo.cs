using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class SerieCorrelativo
{
    public int IdSerieCorrelativo { get; set; }

    public int IdAlmacen { get; set; }

    public int IdTipoDocumentoVenta { get; set; }

    public string Serie { get; set; } = null!;

    public int Correlativo { get; set; }

    public bool Estado { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual TipoDocumentoVentum IdTipoDocumentoVentaNavigation { get; set; } = null!;
}
