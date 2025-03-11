using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Av
{
    public string? Almacén { get; set; }

    public string? Familia { get; set; }

    public string? Línea { get; set; }

    public double? CódigoArtículo { get; set; }

    public string? DescripciónArtículo { get; set; }

    public double? Stock { get; set; }

    public double? PrecioVenta { get; set; }

    public double? Precio { get; set; }

    public double? PrecioS { get; set; }
}
