namespace GSCommerce.Client.Models
{
    public class VentaRegistroDTO
    {
        public VentaCabeceraDTO Cabecera { get; set; } = new VentaCabeceraDTO();
        public List<VentaDetalleDTO> Detalles { get; set; } = new List<VentaDetalleDTO>();
        public List<DetallePagoDTO> Pagos { get; set; } = new List<DetallePagoDTO>();

        public TipoDocumentoVentaDTO TipoDocumento { get; set; } = new TipoDocumentoVentaDTO();

    }
}
