namespace GSCommerce.Client.Models
{
    public class MovimientoDetalleDTO
    {
        public int IdMovimiento { get; set; }

        public int Item { get; set; }

        public string IdArticulo { get; set; } = string.Empty;

        public string DescripcionArticulo { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal Valor { get; set; }
    }
}