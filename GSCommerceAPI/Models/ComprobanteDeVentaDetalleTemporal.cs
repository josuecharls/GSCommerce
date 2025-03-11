using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class ComprobanteDeVentaDetalleTemporal
{
    public int IdComprobante { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public decimal Total { get; set; }
}
