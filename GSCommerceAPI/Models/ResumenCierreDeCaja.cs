using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class ResumenCierreDeCaja
{
    public int IdResumen { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public DateOnly Fecha { get; set; }

    public int IdGrupo { get; set; }

    public string Grupo { get; set; } = null!;

    public string Detalle { get; set; } = null!;

    public decimal Monto { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
