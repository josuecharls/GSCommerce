namespace GSCommerce.Client.Models
{
    public class ClienteDTO
    {
        public int IdCliente { get; set; }

        public string TipoDocumento { get; set; } = null!;

        public string Dniruc { get; set; } = null!;

        public string Nombre { get; set; } = null!;

        public string? Direccion { get; set; }

        public string? Dpd { get; set; }

        public string? Telefono { get; set; }

        public string? Celular { get; set; }

        public string? Email { get; set; }

        public bool Estado { get; set; }
    }
}
