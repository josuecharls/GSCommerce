using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class AlmacenRepuesto
{
    public int IdAlmacenRepuesto { get; set; }

    public int IdAlmacen { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Direccion { get; set; }

    public bool Estado { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual ICollection<StockAlmacenRepuesto> StockAlmacenRepuestos { get; set; } = new List<StockAlmacenRepuesto>();
}
