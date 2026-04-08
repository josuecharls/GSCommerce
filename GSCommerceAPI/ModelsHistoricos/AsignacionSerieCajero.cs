using System;
using System.Collections.Generic;

namespace GSCommerceAPI.ModelsHistoricos;

public partial class AsignacionSerieCajero
{
    public int IdAsignacion { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public int IdSerieCorrelativo { get; set; }
}
