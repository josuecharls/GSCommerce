namespace GSCommerce.Client.Models
{
    public class AlmacenCuentaDTO
    {
        public int IdAlmacenCuenta { get; set; }
        public int IdAlmacen { get; set; }
        public string Banco { get; set; } = string.Empty;
        public string Cuenta { get; set; } = string.Empty;
        public string Cci { get; set; } = string.Empty;
    }
}