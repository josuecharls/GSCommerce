namespace GSCommerce.Client.Models
{
    public class GenerarIngresoResult
    {
        public bool Success { get; set; }

        public int? IdMovimiento { get; set; }

        public string? Estado { get; set; }

        public string? ErrorMessage { get; set; }
    }
}