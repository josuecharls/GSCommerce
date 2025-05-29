namespace GSCommerceAPI.Models.DTOs
{
    public class VentaRegistroDTO
    {
        public ComprobanteDeVentaCabecera Cabecera { get; set; } = new();
        public List<ComprobanteDeVentaDetalle> Detalles { get; set; } = new();
        public List<DetallePagoVentum> Pagos { get; set; } = new();
        public TipoDocumentoVentum TiposDocumentos { get; set; } = new();
    }
}
