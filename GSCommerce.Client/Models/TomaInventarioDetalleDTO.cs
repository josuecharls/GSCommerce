namespace GSCommerce.Client.Models
{
    public class TomaInventarioDetalleDTO
    {
        public int IdTomaInventarioDetalle { get; set; }
        public int IdTomaInventario { get; set; }
        public string IdArticulo { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public bool Estado { get; set; }
        public int? Sobrante { get; set; }
        public int? Faltante { get; set; }
        public string? Nombre { get; set; }
    }
}