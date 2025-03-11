using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDetalleGuiaRemision1
{
    public int IdMovimiento { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string DescripcionArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public string UnidadAlmacen { get; set; } = null!;
}
