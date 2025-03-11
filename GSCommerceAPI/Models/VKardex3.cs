using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VKardex3
{
    public int IdKardex { get; set; }

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public string Familia { get; set; } = null!;

    public string Linea { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public string Articulo { get; set; } = null!;

    public decimal PrecioCompra { get; set; }

    public decimal? PrecioVenta { get; set; }

    public DateOnly? Fecha { get; set; }

    public string Operacion { get; set; } = null!;

    public int SaldoInicial { get; set; }

    public decimal? ValorizadoInicial { get; set; }

    public int Entrada { get; set; }

    public decimal? ValorizadoEntrada { get; set; }

    public int Salida { get; set; }

    public decimal? ValorizadoSalida { get; set; }

    public int SaldoFinal { get; set; }

    public decimal? ValorizadoFinal { get; set; }

    public decimal? ValorizadoFinalPc { get; set; }

    public decimal? ValorizadoFinalPv { get; set; }
}
