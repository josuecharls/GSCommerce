using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GSCommerceAPI.Models;

public partial class MetaMensual
{
    public int IdMetaMensual { get; set; }

    public int IdAlmacen { get; set; }

    public int Anio { get; set; }

    public int Mes { get; set; }

    [Column("MetaMensual")]
    public decimal Meta { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public int IdUsuarioCreacion { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public int? IdUsuarioAprobacion { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public string? Observaciones { get; set; }

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual ICollection<MetaMensualDetalle> MetaMensualDetalles { get; set; } = new List<MetaMensualDetalle>();
}

