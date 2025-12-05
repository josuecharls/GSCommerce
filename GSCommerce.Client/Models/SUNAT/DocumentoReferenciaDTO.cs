namespace GSCommerce.Client.Models.SUNAT
{
    public class DocumentoReferenciaDTO
    {
        public string TipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime? FechaEmision { get; set; }
    }
}
