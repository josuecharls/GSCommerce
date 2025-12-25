namespace GSCommerce.Client.Models.SUNAT;

public class GuiaRemisionSunatDTO
{
    public int IdGuiaRemision { get; set; }
    
    // Datos del documento
    public string Serie { get; set; } = string.Empty;
    public int Numero { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public string? Observaciones { get; set; }
    
    // Datos del remitente (emisor)
    public string RucRemitente { get; set; } = string.Empty;
    public string RazonSocialRemitente { get; set; } = string.Empty;
    public string DireccionRemitente { get; set; } = string.Empty;
    public string UbigeoRemitente { get; set; } = string.Empty;
    public string DepartamentoRemitente { get; set; } = string.Empty;
    public string ProvinciaRemitente { get; set; } = string.Empty;
    public string DistritoRemitente { get; set; } = string.Empty;
    
    // Datos del destinatario
    public string TipoDocumentoDestinatario { get; set; } = "6"; // 6=RUC, 1=DNI
    public string NumeroDocumentoDestinatario { get; set; } = string.Empty;
    public string RazonSocialDestinatario { get; set; } = string.Empty;
    public string DireccionDestinatario { get; set; } = string.Empty;
    public string UbigeoDestinatario { get; set; } = string.Empty;
    
    // Motivo de traslado
    public string MotivoTraslado { get; set; } = "01";
    public string DescripcionMotivoTraslado { get; set; } = string.Empty;
    
    // Datos del traslado
    public decimal PesoBrutoTotal { get; set; }
    public string UnidadMedidaPeso { get; set; } = "KGM";
    public int? NumeroBultos { get; set; }
    public string ModalidadTraslado { get; set; } = "02";
    public DateTime FechaInicioTraslado { get; set; } = DateTime.Now;
    
    // Transportista (si modalidad = 01)
    public string? RucTransportista { get; set; }
    public string? RazonSocialTransportista { get; set; }
    
    // Vehículo (si modalidad = 02)
    public string? PlacaVehiculo { get; set; }
    
    // Conductor (si modalidad = 02)
    public string? TipoDocumentoConductor { get; set; }
    public string? NumeroDocumentoConductor { get; set; }
    public string? NombreConductor { get; set; }
    public string? ApellidoConductor { get; set; }
    public string? LicenciaConductor { get; set; }
    
    // Puntos de partida y llegada
    public string UbigeoPartida { get; set; } = string.Empty;
    public string DireccionPartida { get; set; } = string.Empty;
    public string UbigeoLlegada { get; set; } = string.Empty;
    public string DireccionLlegada { get; set; } = string.Empty;
    
    // Control
    public int? IdAlmacen { get; set; }
    public int? IdUsuario { get; set; }
    public string Estado { get; set; } = "P";
    
    // Estado SUNAT
    public string? EstadoSunat { get; set; }
    public string? HashSunat { get; set; }
    public bool EnviadoSunat { get; set; }
    public string? TicketSunat { get; set; }
    public string? RespuestaSunat { get; set; }
    
    // Detalles
    public List<GuiaRemisionDetalleItemDTO> Detalles { get; set; } = new();

    public string NumeroFormateado => $"{Serie}-{Numero:D8}";
}

public class GuiaRemisionDetalleItemDTO
{
    public int Item { get; set; }
    public string IdArticulo { get; set; } = string.Empty;
    public string DescripcionArticulo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "NIU";
    public decimal? PesoUnitario { get; set; }
}

public class CatalogoItemDTO
{
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

public class GuiaRemisionListadoResponse
{
    public List<GuiaRemisionSunatDTO> Data { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}


