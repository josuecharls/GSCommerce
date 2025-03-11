using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoAperturaCierre1
{
    public int IdAperturaCierre { get; set; }

    public int IdAlmacen { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdUsuario { get; set; }

    public string Cajero { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public decimal SaldoInicial { get; set; }

    public decimal VentaDia { get; set; }

    public decimal Ingresos { get; set; }

    public decimal Egresos { get; set; }

    public decimal SaldoFinal { get; set; }

    public string Estado { get; set; } = null!;

    public string? ObservacionApertura { get; set; }

    public string? ObservacionCierre { get; set; }
}
