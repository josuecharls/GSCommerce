namespace GSCommerceAPI.Models.SUNAT.DTOs
{
    public class ComprobanteCabeceraDTO
    {
        public int IdComprobante { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime FechaEmision { get; set; }
        public TimeSpan HoraEmision { get; set; }

        public string Moneda { get; set; } = "PEN";
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public string MontoLetras { get; set; } = string.Empty;
        public string RucEmisor { get; set; } = string.Empty;
        public string RazonSocialEmisor { get; set; } = string.Empty;
        public string DireccionEmisor { get; set; } = string.Empty;
        public string UbigeoEmisor { get; set; } = string.Empty;
        public string DepartamentoEmisor { get; set; } = string.Empty;
        public string ProvinciaEmisor { get; set; } = string.Empty;
        public string DistritoEmisor { get; set; } = string.Empty;

        public string DocumentoCliente { get; set; } = string.Empty;
        public string TipoDocumentoCliente { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string DireccionCliente { get; set; } = string.Empty;
        public List<ComprobanteDetalleDTO> Detalles { get; set; } = new();
        public string TipoNotaCredito { get; set; } = string.Empty;
        public string TipoDocumentoReferencia { get; set; } = string.Empty;
        public string SerieDocumentoReferencia { get; set; } = string.Empty;
        public int NumeroDocumentoReferencia { get; set; }
        public string DescripcionNotaCredito { get; set; } = string.Empty;
    }
}
