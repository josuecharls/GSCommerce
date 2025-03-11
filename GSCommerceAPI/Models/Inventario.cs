using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Inventario
{
    public int InventarioId { get; set; }

    public DateTime? Fecha { get; set; }

    public string? IdArticulo { get; set; }

    public int? IdAlmacen { get; set; }
}
