using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class IngresosEgresosDetalle
{
    public int IdDetalleIngresoEgreso { get; set; }

    public int IdIngresoEgreso { get; set; }

    public string Forma { get; set; } = null!;

    public string Detalle { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Banco { get; set; }

    public string? Cuenta { get; set; }

    public byte[]? Imagen { get; set; }

    public virtual IngresosEgresosCabecera IdIngresoEgresoNavigation { get; set; } = null!;
}
