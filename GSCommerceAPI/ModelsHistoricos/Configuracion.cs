using System;
using System.Collections.Generic;

namespace GSCommerceAPI.ModelsHistoricos;

public partial class Configuracion
{
    public string Configuracion1 { get; set; } = null!;

    public string Valor { get; set; } = null!;

    public string? Descripcion { get; set; }
}
