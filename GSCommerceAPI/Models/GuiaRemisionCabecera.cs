using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class GuiaRemisionCabecera
{
    public int IdGuiaRemision { get; set; }

    public string? Serie { get; set; }

    public int? Numero { get; set; }

    public DateTime? Fecha { get; set; }

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
    public string? MotivoTraslado { get; set; }
    public string? DescripcionMotivoTraslado { get; set; }
    public string? ModalidadTraslado { get; set; }
    public DateTime? FechaInicioTraslado { get; set; }
    public decimal? PesoBrutoTotal { get; set; }
    public string? UnidadMedidaPeso { get; set; }
    public int? NumeroBultos { get; set; }
    
    public string? RucTransportista { get; set; }
    public string? RazonSocialTransportista { get; set; }
    
    public string? PlacaVehiculo { get; set; }
    
    public string? TipoDocumentoConductor { get; set; }
    public string? NumeroDocumentoConductor { get; set; }
    public string? NombreConductor { get; set; }
    public string? ApellidoConductor { get; set; }
    public string? LicenciaConductor { get; set; }

    public virtual ICollection<GuiaRemisionDetalle> GuiaRemisionDetalles { get; set; } = new List<GuiaRemisionDetalle>();
}
