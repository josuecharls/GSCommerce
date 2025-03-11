using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class NotaDeCreditoCabecera
{
    public int IdNc { get; set; }

    public int IdTipoDocumento { get; set; }

    public string Serie { get; set; } = null!;

    public int Numero { get; set; }

    public DateTime Fecha { get; set; }

    public string Referencia { get; set; } = null!;

    public string IdMotivo { get; set; } = null!;

    public int IdCliente { get; set; }

    public string Dniruc { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Direccion { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public decimal Redondeo { get; set; }

    public decimal Afavor { get; set; }

    public int IdUsuario { get; set; }

    public int IdAlmacen { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaHoraRegistro { get; set; }

    public bool? Empleada { get; set; }

    public int? IdUsuarioAnula { get; set; }

    public DateTime? FechaHoraUsuarioAnula { get; set; }

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual TipoDocumentoVentum IdTipoDocumentoNavigation { get; set; } = null!;

    public virtual ICollection<NotaDeCreditoDetalle> NotaDeCreditoDetalles { get; set; } = new List<NotaDeCreditoDetalle>();
}
