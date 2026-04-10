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

        // Resumen para vista (puede venir null si el endpoint no lo llena)
        public int? TotalArticulos { get; set; }
        public int? TotalUnidades { get; set; }
    }
}
