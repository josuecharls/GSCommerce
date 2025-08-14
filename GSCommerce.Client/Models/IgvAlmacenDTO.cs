namespace GSCommerce.Client.Models
{
    public class IgvAlmacenDTO
    {
        public int IdAlmacen { get; set; }
        public decimal Igv { get; set; } = 18m; // Porcentaje de IGV (0 para exonerado)
    }
}