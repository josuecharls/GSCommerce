namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ComparativoVendedorDTO
    {
        public string Vendedor { get; set; } = string.Empty;
        public int VentasHoy { get; set; }
        public int ClientesHoy { get; set; }
        public decimal MontoHoy { get; set; }
        public int VentasOtraFecha { get; set; }
        public int ClientesOtraFecha { get; set; }
        public decimal MontoOtraFecha { get; set; }
    }
}