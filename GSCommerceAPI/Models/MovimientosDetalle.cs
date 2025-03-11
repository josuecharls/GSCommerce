using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class MovimientosDetalle
{
    public int IdMovimiento { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string DescripcionArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal Valor { get; set; }

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;

    public virtual MovimientosCabecera IdMovimientoNavigation { get; set; } = null!;
}
