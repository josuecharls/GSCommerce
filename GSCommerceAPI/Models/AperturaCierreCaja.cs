using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class AperturaCierreCaja
{
    public int IdAperturaCierre { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal FondoFijo { get; set; }

    public decimal SaldoInicial { get; set; }

    public decimal VentaDia { get; set; }

    public decimal Ingresos { get; set; }

    public decimal Egresos { get; set; }

    public decimal SaldoFinal { get; set; }

    public string Estado { get; set; } = null!;

    public string? ObservacionApertura { get; set; }

    public string? ObservacionCierre { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
