namespace GSCommerce.Client.Models
{
    public class AlmacenDTO
    {
        public int IdAlmacen { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public bool EsTienda { get; set; }

        public string Direccion { get; set; } = string.Empty;

        public string? Dpd { get; set; }

        public string? Telefono { get; set; }

        public string? Celular { get; set; }

        public string? RazonSocial { get; set; }

        public string? Ruc { get; set; }

        public bool Estado { get; set; }

    }
}
