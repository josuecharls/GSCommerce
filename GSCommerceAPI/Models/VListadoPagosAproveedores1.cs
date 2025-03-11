using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoPagosAproveedores1
{
    public int? IdProveedor { get; set; }

    public DateTime Fecha { get; set; }

    public string Glosa { get; set; } = null!;

    public string Forma { get; set; } = null!;

    public string Detalle { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Banco { get; set; }

    public string? Cuenta { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public byte[]? Imagen { get; set; }

    public string Almacén { get; set; } = null!;
}
