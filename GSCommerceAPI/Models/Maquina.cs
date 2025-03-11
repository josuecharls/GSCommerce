using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Maquina
{
    public int IdPc { get; set; }

    public int IdAlmacen { get; set; }

    public int IdUsuario { get; set; }

    public string Mac { get; set; } = null!;

    public string Anydesk { get; set; } = null!;
}
