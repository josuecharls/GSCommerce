using System;
using System.Collections.Generic;

namespace GSCommerceAPI.ModelsHistoricos;

public partial class VRecaudacion3
{
    public DateOnly? Fecha { get; set; }

    public int IdAlmacen { get; set; }

    public int IdCajero { get; set; }

    public string? Descripcion { get; set; }

    public decimal? Monto { get; set; }
}
