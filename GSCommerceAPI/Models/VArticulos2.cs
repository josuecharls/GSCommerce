using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VArticulos2
{
    public int IdAlmacen { get; set; }

    public int Almacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int? Stock { get; set; }

    public decimal Precio { get; set; }

    public bool Estado { get; set; }
}
