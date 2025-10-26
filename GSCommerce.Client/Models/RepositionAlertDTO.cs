namespace GSCommerce.Client.Models
{
    public class RepositionAlertDTO
    {
        public string IdArticulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public int StockPrincipal { get; set; }
        public List<AlmacenStockDTO> AlmacenesConStock { get; set; } = new();
    }

    public class AlmacenStockDTO
    {
        public int IdAlmacen { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}


