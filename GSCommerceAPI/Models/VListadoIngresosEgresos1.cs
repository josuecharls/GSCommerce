using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoIngresosEgresos1
{
    public int IdIngresoEgreso { get; set; }

    public int IdUsuario { get; set; }

    public string Cajero { get; set; } = null!;

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public string Naturaleza { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public string Glosa { get; set; } = null!;

    public decimal Monto { get; set; }

    public int? IdAlmacenDestino { get; set; }

    public int? IdCajeroDestino { get; set; }

    public string Estado { get; set; } = null!;
}
