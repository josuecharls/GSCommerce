namespace GSCommerce.Client.Models.SUNAT
{
    public class GuiaRemisionDTO
    {
        public int IdGuiaRemision { get; set; }
        public int IdMovimiento { get; set; }
        
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        
        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }
        
        public string MotivoTraslado { get; set; } = string.Empty;
        public string DescripcionMotivo { get; set; } = string.Empty;
        
        public decimal? PesoTotal { get; set; }
        public int? NumeroBultos { get; set; }
        
        public string RucRemitente { get; set; } = string.Empty;
        public string RazonSocialRemitente { get; set; } = string.Empty;
        public string DireccionRemitente { get; set; } = string.Empty;
        public string UbigeoRemitente { get; set; } = string.Empty;
        public string DepartamentoRemitente { get; set; } = string.Empty;
        public string ProvinciaRemitente { get; set; } = string.Empty;
        public string DistritoRemitente { get; set; } = string.Empty;
        
        public string TipoDocumentoDestinatario { get; set; } = string.Empty;
        public string NumeroDocumentoDestinatario { get; set; } = string.Empty;
        public string RazonSocialDestinatario { get; set; } = string.Empty;
        public string DireccionDestinatario { get; set; } = string.Empty;
        public string UbigeoDestinatario { get; set; } = string.Empty;
        public string DepartamentoDestinatario { get; set; } = string.Empty;
        public string ProvinciaDestinatario { get; set; } = string.Empty;
        public string DistritoDestinatario { get; set; } = string.Empty;
        
        public string DireccionOrigen { get; set; } = string.Empty;
        public string UbigeoOrigen { get; set; } = string.Empty;
        public string DireccionDestino { get; set; } = string.Empty;
        public string UbigeoDestino { get; set; } = string.Empty;
        
        public TransporteDTO? Transporte { get; set; }
        
        public List<GuiaRemisionDetalleDTO> Detalles { get; set; } = new();
        
        public List<DocumentoReferenciaDTO> DocumentosReferencia { get; set; } = new();
        
        public string Observaciones { get; set; } = string.Empty;
        
        public bool EnviadoSunat { get; set; }
        public string? TicketSunat { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaRespuestaSunat { get; set; }
        public string? RespuestaSunat { get; set; }
        public string? EstadoSunat { get; set; }
        public string? CodigoRespuestaSunat { get; set; }
        public string? MensajeRespuestaSunat { get; set; }
        public string? Hash { get; set; }
    }
}
