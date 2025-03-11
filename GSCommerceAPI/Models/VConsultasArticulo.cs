using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VConsultasArticulo
{
    public string Nombre { get; set; } = null!;

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal PrecioCompra { get; set; }

    public string? FechaVenta { get; set; }

    public DateOnly? FechaCompra { get; set; }

    public int? Cantidad { get; set; }

    public decimal? PrecioVentaMedio { get; set; }

    public decimal? Monto { get; set; }
}
