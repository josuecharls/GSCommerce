using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDetallePagoVenta2
{
    public DateOnly? Fecha { get; set; }

    public int IdComprobante { get; set; }

    public string Serie { get; set; } = null!;

    public int Numero { get; set; }

    public int IdTipoDocumento { get; set; }

    public int IdCajero { get; set; }

    public int IdAlmacen { get; set; }

    public string Estado { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Soles { get; set; }

    public decimal Dolares { get; set; }

    public decimal? Vuelto { get; set; }
}
