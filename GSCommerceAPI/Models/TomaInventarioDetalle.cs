using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class TomaInventarioDetalle
{
    public int IdTomaInventarioDetalle { get; set; }

    public int IdTomaInventario { get; set; }

    public string IdArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public bool Estado { get; set; }

    public int? Sobrante { get; set; }

    public int? Faltante { get; set; }

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;

    public virtual TomaInventario IdTomaInventarioNavigation { get; set; } = null!;
}
