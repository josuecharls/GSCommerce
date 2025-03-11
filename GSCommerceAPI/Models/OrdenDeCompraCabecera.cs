using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class OrdenDeCompraCabecera
{
    public int IdOc { get; set; }

    public int IdProveedor { get; set; }

    public string NumeroOc { get; set; } = null!;

    public DateTime FechaOc { get; set; }

    public string Rucproveedor { get; set; } = null!;

    public string NombreProveedor { get; set; } = null!;

    public string DireccionProveedor { get; set; } = null!;

    public string Moneda { get; set; } = null!;

    public decimal TipoCambio { get; set; }

    public string FormaPago { get; set; } = null!;

    public bool SinIgv { get; set; }

    public DateTime FechaEntrega { get; set; }

    public string Atencion { get; set; } = null!;

    public string Glosa { get; set; } = null!;

    public decimal ImporteSubTotal { get; set; }

    public decimal ImporteIgv { get; set; }

    public decimal ImporteTotal { get; set; }

    public bool EstadoEmision { get; set; }

    public DateTime? FechaEmision { get; set; }

    public string EstadoAtencion { get; set; } = null!;

    public DateTime? FechaAtencionTotal { get; set; }

    public DateTime? FechaAnulado { get; set; }

    public DateTime? FechaCierre { get; set; }

    public int? IdUsuarioRegistra { get; set; }

    public DateTime FechaRegistra { get; set; }

    public virtual Usuario? IdUsuarioRegistraNavigation { get; set; }

    public virtual ICollection<MovimientosCabecera> MovimientosCabeceras { get; set; } = new List<MovimientosCabecera>();

    public virtual ICollection<OrdenDeCompraDetalle> OrdenDeCompraDetalles { get; set; } = new List<OrdenDeCompraDetalle>();
}
