using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class TipoPagoVentum
{
    public int IdTipoPagoVenta { get; set; }

    public string Tipo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public bool Estado { get; set; }

    public virtual ICollection<DetallePagoVentum> DetallePagoVenta { get; set; } = new List<DetallePagoVentum>();
}
