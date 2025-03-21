namespace GSCommerce.Client.Models
{
    public class ProveedorDTO
    {
        public int IdProveedor { get; set; }

        public string? TipoDocumento { get; set; }

        public string? Ruc { get; set; }

        public string? TipoPersona { get; set; }

        public string Nombre { get; set; } = null!;

        public string? NombreComercial { get; set; }

        public string? Direccion { get; set; }

        public string? Dpd { get; set; }

        public string? Pais { get; set; }

        public string? FormaPago { get; set; }

        public string? Banco { get; set; }

        public string? Cuenta { get; set; }

        public string? Cci { get; set; }

        public string? Contacto { get; set; }

        public string? Telefono { get; set; }

        public string? Celular { get; set; }

        public bool Estado { get; set; }
    }
}
