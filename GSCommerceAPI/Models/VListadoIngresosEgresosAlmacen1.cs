using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoIngresosEgresosAlmacen1
{
    public int IdMovimiento { get; set; }

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Motivo { get; set; } = null!;

    public DateOnly? Fecha { get; set; }

    public string Descripcion { get; set; } = null!;

    public int? IdGuiaRemision { get; set; }

    public int? IdUsuarioConfirma { get; set; }

    public string Estado { get; set; } = null!;
}
