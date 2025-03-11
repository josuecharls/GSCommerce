using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VArticulos1
{
    public string IdArticulo { get; set; } = null!;

    public int IdProveedor { get; set; }

    public string Descripcion { get; set; } = null!;

    public string UnidadAlmacen { get; set; } = null!;

    public decimal PrecioVenta { get; set; }

    public bool Estado { get; set; }
}
