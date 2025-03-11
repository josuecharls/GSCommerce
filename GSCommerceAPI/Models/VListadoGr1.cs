using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VListadoGr1
{
    public int IdGuiaRemision { get; set; }

    public int IdMovimiento { get; set; }

    public string? Numero { get; set; }

    public DateOnly? Fecha { get; set; }

    public string? DireccionOrigen { get; set; }

    public string? Dpdorigen { get; set; }

    public string? TipoDestinatario { get; set; }

    public string? RazonSocialNombre { get; set; }

    public string? Rucdnice { get; set; }

    public string? DireccionDestino { get; set; }

    public string? Dpddestino { get; set; }

    public string? Observaciones { get; set; }

    public int? IdUsuario { get; set; }

    public string? Estado { get; set; }

    public int? IdAlmacen { get; set; }

    public int? IdProveedor { get; set; }

    public int? IdTipoDocumento { get; set; }

    public int? IdAlmacenDestino { get; set; }

    public string? AlmacenOrigen { get; set; }

    public string? AlmacenDestino { get; set; }
}
