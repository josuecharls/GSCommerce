namespace GSCommerceAPI.Models.SUNAT.DTOs
{
    public class EnvioFacturaRequest
    {
        public string RucEmisor { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public string TipoDocumento { get; set; } = "01"; // 01 = factura
        public string UsuarioSOL { get; set; } = string.Empty;
        public string ClaveSOL { get; set; } = string.Empty;
    }
}
