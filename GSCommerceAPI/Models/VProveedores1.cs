using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VProveedores1
{
    public int IdProveedor { get; set; }

    public string? Ruc { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Estado { get; set; }
}
