using System;
using System.Collections.Generic;

namespace GSCommerceAPI.ModelsHistoricos;

public partial class Maquina
{
    public int IdPc { get; set; }

    public int IdAlmacen { get; set; }

    public int IdUsuario { get; set; }

    public string Mac { get; set; } = null!;

    public string Anydesk { get; set; } = null!;
}
