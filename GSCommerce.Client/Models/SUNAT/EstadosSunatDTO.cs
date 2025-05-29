namespace GSCommerce.Client.Models.SUNAT
{
    public class EstadosSunatDTO
    {
        public int IdComprobante { get; set; }
        public string TipoDocumento { get; set; } = "";
        public string Serie { get; set; } = "";
        public int Numero { get; set; }
        public DateTime FechaEmision { get; set; }
        public string EstadoSunat { get; set; } = ""; // ACEPTADO, RECHAZADO, PENDIENTE
        public string? DescripcionSunat { get; set; }
    }
}
