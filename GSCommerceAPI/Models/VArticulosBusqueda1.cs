using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VArticulosBusqueda1
{
    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Proveedor { get; set; } = null!;

    public bool Estado { get; set; }
}
