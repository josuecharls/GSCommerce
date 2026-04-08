namespace GSCommerce.Client.Models
{
    public class AlmacenRepuestoDTO
    {
        public int IdAlmacenRepuesto { get; set; }
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public bool Estado { get; set; }
    }
}
