using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Kardex
{
    public int IdKardex { get; set; }

    public int IdAlmacen { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string TipoMovimiento { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public int SaldoInicial { get; set; }

    public int Cantidad { get; set; }

    public int SaldoFinal { get; set; }

    public decimal Valor { get; set; }

    public string Origen { get; set; } = null!;

    public bool? NoKardexGeneral { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;
}
