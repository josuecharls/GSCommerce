using System;
using System.Collections.Generic;

namespace GSCommerceAPI.ModelsHistoricos;

public partial class VUsuarioAperturaAlmacen1
{
    public int IdUsuario { get; set; }

    public string Cajero { get; set; } = null!;

    public int IdAlmacen { get; set; }

    public string Estado { get; set; } = null!;

    public DateOnly Fecha { get; set; }
}
