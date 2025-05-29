namespace GSCommerce.Client.Models
{
    public class AsignacionSerieCajeroDTO
    {
        public int IdUsuario { get; set; }
        public int IdAlmacen { get; set; }
        public List<int> IdSeries { get; set; } = new();
    }
}
