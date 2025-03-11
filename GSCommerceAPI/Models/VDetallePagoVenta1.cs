using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDetallePagoVenta1
{
    public int IdDetallePagoVenta { get; set; }

    public int IdComprobante { get; set; }

    public int IdTipoPagoVenta { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Soles { get; set; }

    public decimal Dolares { get; set; }

    public string? Datos { get; set; }

    public decimal? Vuelto { get; set; }

    public decimal? PorcentajeTarjetaSoles { get; set; }

    public decimal? PorcentajeTarjetaDolares { get; set; }
}
