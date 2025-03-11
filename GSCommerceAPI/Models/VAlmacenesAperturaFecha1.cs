using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VAlmacenesAperturaFecha1
{
    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public string Estado { get; set; } = null!;
}
