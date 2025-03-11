using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VentasBak09112020
{
    public int IdComprobante { get; set; }

    public int IdTipoDocumento { get; set; }

    public string Serie { get; set; } = null!;

    public int Numero { get; set; }

    public DateTime Fecha { get; set; }

    public int? IdCliente { get; set; }

    public string? Dniruc { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Direccion { get; set; }

    public decimal TipoCambio { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public decimal? Redondeo { get; set; }

    public decimal? Apagar { get; set; }

    public int IdVendedor { get; set; }

    public int IdCajero { get; set; }

    public int IdAlmacen { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaHoraRegistro { get; set; }

    public int? IdUsuarioAnula { get; set; }

    public DateTime? FechaHoraUsuarioAnula { get; set; }

    public string? GeneroNc { get; set; }
}
