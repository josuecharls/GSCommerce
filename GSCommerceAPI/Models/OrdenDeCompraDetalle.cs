using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class OrdenDeCompraDetalle
{
    public int IdOc { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string DescripcionArticulo { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal Total { get; set; }

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;

    public virtual OrdenDeCompraCabecera IdOcNavigation { get; set; } = null!;
}
