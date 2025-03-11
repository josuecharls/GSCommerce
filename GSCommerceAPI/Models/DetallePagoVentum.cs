using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class DetallePagoVentum
{
    public int IdDetallePagoVenta { get; set; }

    public int IdComprobante { get; set; }

    public int IdTipoPagoVenta { get; set; }

    public decimal Soles { get; set; }

    public decimal Dolares { get; set; }

    public string? Datos { get; set; }

    public decimal? Vuelto { get; set; }

    public decimal? PorcentajeTarjetaSoles { get; set; }

    public decimal? PorcentajeTarjetaDolares { get; set; }

    public virtual ComprobanteDeVentaCabecera IdComprobanteNavigation { get; set; } = null!;

    public virtual TipoPagoVentum IdTipoPagoVentaNavigation { get; set; } = null!;
}
