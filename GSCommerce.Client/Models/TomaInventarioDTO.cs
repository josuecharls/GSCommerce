namespace GSCommerce.Client.Models
{
    public class TomaInventarioDTO
    {
        public int IdTomaInventario { get; set; }
        public int IdAlmacen { get; set; }
        public DateTime Fecha { get; set; }
        public string EstadoToma { get; set; } = string.Empty;
        public bool Estado { get; set; }
        public string? Intervienen { get; set; }
        public string? Nombre { get; set; }
    }
}