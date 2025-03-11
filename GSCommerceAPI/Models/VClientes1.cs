using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VClientes1
{
    public int IdCliente { get; set; }

    public string Dniruc { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Direccion { get; set; }

    public bool Estado { get; set; }
}
