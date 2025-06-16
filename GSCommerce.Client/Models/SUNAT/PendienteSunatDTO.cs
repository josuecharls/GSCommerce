namespace GSCommerce.Client.Models.SUNAT
{
    public class PendienteSunatDTO
    {
        public int IdFe { get; set; }
        public string Tienda { get; set; } = string.Empty;
        public string TipoDoc { get; set; } = string.Empty;
        public string? Numero { get; set; }
        public string? Fecha { get; set; }
        public decimal? Apagar { get; set; }
        public string Hash { get; set; } = string.Empty;
        public bool EnviadoSunat { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaRespuestaSunat { get; set; }
        public string? RespuestaSunat { get; set; }
        public string? TicketSunat { get; set; }
        public string? Xml { get; set; }
        public int IdComprobante { get; set; }
    }
}
