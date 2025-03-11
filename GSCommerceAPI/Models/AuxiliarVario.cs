using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class AuxiliarVario
{
    public int IdAuxiliar { get; set; }

    public string Auxiliar { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Alias { get; set; } = null!;

    public bool Estado { get; set; }
}
