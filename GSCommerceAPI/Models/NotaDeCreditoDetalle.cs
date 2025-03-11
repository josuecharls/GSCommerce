using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class NotaDeCreditoDetalle
{
    public int IdNc { get; set; }

    public int Item { get; set; }

    public string? IdArticulo { get; set; }

    public string Descripcion { get; set; } = null!;

    public string? UnidadMedida { get; set; }

    public int? Cantidad { get; set; }

    public decimal? Precio { get; set; }

    public decimal? PorcentajeDescuento { get; set; }

    public decimal Total { get; set; }

    public virtual NotaDeCreditoCabecera IdNcNavigation { get; set; } = null!;
}
