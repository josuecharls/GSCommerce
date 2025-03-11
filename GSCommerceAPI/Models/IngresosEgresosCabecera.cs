using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class IngresosEgresosCabecera
{
    public int IdIngresoEgreso { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public string Naturaleza { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public string Glosa { get; set; } = null!;

    public decimal Monto { get; set; }

    public int? IdProveedor { get; set; }

    public int? IdAlmacenDestino { get; set; }

    public int? IdCajeroDestino { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public string Estado { get; set; } = null!;

    public int? IdAlmacenGasto { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<IngresosEgresosDetalle> IngresosEgresosDetalles { get; set; } = new List<IngresosEgresosDetalle>();
}
