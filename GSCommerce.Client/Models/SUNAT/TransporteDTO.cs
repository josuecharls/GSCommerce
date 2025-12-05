namespace GSCommerce.Client.Models.SUNAT
{
    public class TransporteDTO
    {
        public string? NumeroPlaca { get; set; }
        public string? NumeroDocumentoConductor { get; set; }
        public string? TipoDocumentoConductor { get; set; }
        public string? NombreConductor { get; set; }
        public string? NumeroLicencia { get; set; }
        public string? MarcaVehiculo { get; set; }
        public string? ModeloVehiculo { get; set; }
        public bool EsTransportePrivado { get; set; } = true;
    }
}