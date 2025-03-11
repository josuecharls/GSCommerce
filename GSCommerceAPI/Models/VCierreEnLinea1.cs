using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VCierreEnLinea1
{
    public DateOnly? Fecha { get; set; }

    public int IdAlmacen { get; set; }

    public int IdUsuario { get; set; }

    public string Categoria { get; set; } = null!;

    public decimal? Monto { get; set; }
}
