namespace GSCommerce.Client.Models
{
    public class TipoDocumentoVentaDTO
    {
        public int IdTipoDocumentoVenta { get; set; }

        public string Descripcion { get; set; } = null!;

        public string Abreviatura { get; set; } = null!;

        public bool Manual { get; set; }

        public bool Estado { get; set; }
    }
}
