using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class TomaInventario
{
    public int IdTomaInventario { get; set; }

    public int IdAlmacen { get; set; }

    public DateTime Fecha { get; set; }

    public string EstadoToma { get; set; } = null!;

    public bool Estado { get; set; }

    public string? Intervienen { get; set; }

    public virtual Almacen? IdAlmacenNavigation { get; set; } = null!;

    public virtual ICollection<TomaInventarioDetalle> TomaInventarioDetalles { get; set; } = new List<TomaInventarioDetalle>();
}
