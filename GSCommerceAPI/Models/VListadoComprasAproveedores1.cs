using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoComprasAproveedores1
{
    public int IdOc { get; set; }

    public int IdProveedor { get; set; }

    public string NumeroOc { get; set; } = null!;

    public DateTime FechaOc { get; set; }

    public string DescripcionArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal Total { get; set; }

    public string Glosa { get; set; } = null!;
}
