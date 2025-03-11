using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class MovimientosCabecera
{
    public int IdMovimiento { get; set; }

    public int IdAlmacen { get; set; }

    public string Tipo { get; set; } = null!;

    public string Motivo { get; set; } = null!;

    public DateOnly? Fecha { get; set; }

    public string Descripcion { get; set; } = null!;

    public int? IdProveedor { get; set; }

    public int? IdAlmacenDestinoOrigen { get; set; }

    public int? IdOc { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaHoraRegistro { get; set; }

    public int? IdGuiaRemision { get; set; }

    public int? IdUsuarioConfirma { get; set; }

    public DateTime? FechaHoraConfirma { get; set; }

    public string Estado { get; set; } = null!;

    public virtual ICollection<GuiaRemisionCabecera> GuiaRemisionCabeceras { get; set; } = new List<GuiaRemisionCabecera>();

    public virtual Almacen IdAlmacenNavigation { get; set; } = null!;

    public virtual OrdenDeCompraCabecera? IdOcNavigation { get; set; }

    public virtual ICollection<MovimientosDetalle> MovimientosDetalles { get; set; } = new List<MovimientosDetalle>();
}
