using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoVentaDiaria1
{
    public int IdComprobante { get; set; }

    public int IdTipoDocumento { get; set; }

    public DateOnly? Fecha { get; set; }

    public TimeOnly? Hora { get; set; }

    public string Almacen { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Serie { get; set; } = null!;

    public int Numero { get; set; }

    public int? IdCliente { get; set; }

    public string? Dniruc { get; set; }

    public string Nombre { get; set; } = null!;

    public decimal? Total { get; set; }

    public int IdVendedor { get; set; }

    public int IdCajero { get; set; }

    public int IdAlmacen { get; set; }

    public string? Pago { get; set; }

    public string Estado { get; set; } = null!;

    public string? GeneroNc { get; set; }
}
