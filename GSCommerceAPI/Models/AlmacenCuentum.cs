using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class AlmacenCuentum
{
    public int IdAlmacenCuenta { get; set; }

    public int IdAlmacen { get; set; }

    public string Banco { get; set; } = null!;

    public string Cuenta { get; set; } = null!;

    public string Cci { get; set; } = null!;

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;
}
