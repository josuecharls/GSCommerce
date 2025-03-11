using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class ComplementoArticulo
{
    public int IdComplemento { get; set; }

    public string Complemento { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? Alias { get; set; }

    public bool Estado { get; set; }
}
