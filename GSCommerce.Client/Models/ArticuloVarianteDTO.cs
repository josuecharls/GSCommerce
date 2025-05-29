namespace GSCommerce.Client.Models
{
    public class ArticuloVarianteDTO
    {
        public int IdVariante { get; set; }

        public string IdArticulo { get; set; } = null!;

        public string Color { get; set; } = null!;

        public string Talla { get; set; } = null!;

    }
}
