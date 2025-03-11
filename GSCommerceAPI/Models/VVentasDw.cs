using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VVentasDw
{
    public DateTime Fecha { get; set; }

    public string Tienda { get; set; } = null!;

    public string Serie { get; set; } = null!;

    public int Numero { get; set; }

    public string? DocIdentidad { get; set; }

    public string Cliente { get; set; } = null!;

    public decimal SubTotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public decimal? Redondeo { get; set; }

    public decimal? Apagar { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Linea { get; set; } = null!;

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal Precio { get; set; }

    public int Cantidad { get; set; }

    public decimal TotalProducto { get; set; }
}
