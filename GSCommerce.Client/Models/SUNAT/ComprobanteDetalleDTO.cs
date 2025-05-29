namespace GSCommerce.Client.Models.SUNAT
{
    public class ComprobanteDetalleDTO
    {
        public int Item { get; set; }
        public string CodigoItem { get; set; } = string.Empty;
        public string DescripcionItem { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = "NIU";
        public decimal Cantidad { get; set; }

        public decimal PrecioUnitarioSinIGV { get; set; }
        public decimal PrecioUnitarioConIGV { get; set; }
        public decimal TotalSinIGV => Math.Round(Cantidad * PrecioUnitarioSinIGV, 2);
        public decimal IGV { get; set; }
        public decimal Total => TotalSinIGV + IGV ;
    }
}
