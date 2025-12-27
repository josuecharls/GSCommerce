namespace GSCommerceAPI.Models.SUNAT.DTOs;

public class GuiaRemisionCabeceraDTO
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
    
    // Motivo de traslado (Catálogo 20 SUNAT)
    public string MotivoTraslado { get; set; } = "01"; // 01=Venta, 02=Compra, 04=Traslado entre establecimientos, etc.
    public string DescripcionMotivoTraslado { get; set; } = string.Empty;
    
    // Datos del traslado
    public decimal PesoBrutoTotal { get; set; }
    public string UnidadMedidaPeso { get; set; } = "KGM"; // KGM = Kilogramos
    public int? NumeroBultos { get; set; }
    public string ModalidadTraslado { get; set; } = "02"; // 01=Transporte público, 02=Transporte privado
    public DateTime FechaInicioTraslado { get; set; } = DateTime.Now;
    
    // Datos del transportista (si modalidad = 01 Transporte público)
    public string? RucTransportista { get; set; }
    public string? RazonSocialTransportista { get; set; }
    
    // Datos del vehículo (si modalidad = 02 Transporte privado)
    public string? PlacaVehiculo { get; set; }
    
    // Datos del conductor (si modalidad = 02 Transporte privado)
    public string? TipoDocumentoConductor { get; set; } // 1=DNI
    public string? NumeroDocumentoConductor { get; set; }
    public string? NombreConductor { get; set; }
    public string? ApellidoConductor { get; set; }
    public string? LicenciaConductor { get; set; }
    
    // Indicador de vehículo M1/L (autos, motos) - Si es true, no se envían datos del conductor a SUNAT
    public bool EsVehiculoM1L { get; set; } = true;
    
    // Punto de partida
    public string UbigeoPartida { get; set; } = string.Empty;
    public string DireccionPartida { get; set; } = string.Empty;
    
    // Punto de llegada
    public string UbigeoLlegada { get; set; } = string.Empty;
    public string DireccionLlegada { get; set; } = string.Empty;
    
    // Datos de control
    public int? IdAlmacen { get; set; }
    public int? IdUsuario { get; set; }
    public string Estado { get; set; } = "P"; // P=Pendiente, E=Emitido, A=Anulado
    
    // Estado SUNAT
    public string? EstadoSunat { get; set; }
    public string? HashSunat { get; set; }
    public bool EnviadoSunat { get; set; }
    public string? TicketSunat { get; set; }
    public string? RespuestaSunat { get; set; }
    
    // Detalles
    public List<GuiaRemisionDetalleDTO> Detalles { get; set; } = new();
}

public class GuiaRemisionDetalleDTO
{
    public int Item { get; set; }
    public string IdArticulo { get; set; } = string.Empty;
    public string DescripcionArticulo { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "NIU"; // NIU = Unidad, KGM = Kilogramos
    public decimal? PesoUnitario { get; set; }
}

/// <summary>
/// Catálogo 20 SUNAT - Motivos de traslado
/// </summary>
public static class CatalogoMotivoTraslado
{
    public static readonly Dictionary<string, string> Motivos = new()
    {
        { "01", "Venta" },
        { "02", "Compra" },
        { "03", "Venta con entrega a terceros" },
        { "04", "Traslado entre establecimientos de la misma empresa" },
        { "05", "Consignación" },
        { "06", "Devolución" },
        { "07", "Recojo de bienes transformados" },
        { "08", "Importación" },
        { "09", "Exportación" },
        { "13", "Otros" },
        { "14", "Venta sujeta a confirmación del comprador" },
        { "17", "Traslado de bienes para transformación" },
        { "18", "Traslado emisor itinerante CP" }
    };
}

/// <summary>
/// Catálogo 18 SUNAT - Modalidad de transporte
/// </summary>
public static class CatalogoModalidadTransporte
{
    public static readonly Dictionary<string, string> Modalidades = new()
    {
        { "01", "Transporte público" },
        { "02", "Transporte privado" }
    };
}

/// <summary>
/// Catálogo 06 SUNAT - Tipos de documento de identidad
/// </summary>
public static class CatalogoTipoDocumentoIdentidad
{
    public static readonly Dictionary<string, string> Tipos = new()
    {
        { "0", "No domiciliado, sin RUC" },
        { "1", "DNI" },
        { "4", "Carnet de extranjería" },
        { "6", "RUC" },
        { "7", "Pasaporte" },
        { "A", "Cédula diplomática de identidad" },
        { "B", "Doc. identidad país residencia - No domiciliado" }
    };
}


