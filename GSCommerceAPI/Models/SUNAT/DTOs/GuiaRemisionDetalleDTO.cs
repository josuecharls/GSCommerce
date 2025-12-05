namespace GSCommerceAPI.Models.SUNAT.DTOs
{
    public class GuiaRemisionDetalleDTO
    {
        public int Item { get; set; }
        public string CodigoItem { get; set; } = string.Empty;
        public string DescripcionItem { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = "NIU";
        public decimal Cantidad { get; set; }
        public decimal? Peso { get; set; }
    }
}
