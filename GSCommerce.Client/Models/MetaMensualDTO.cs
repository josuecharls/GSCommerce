namespace GSCommerce.Client.Models
{
    public class MetaMensualDTO
    {
        public int IdMetaMensual { get; set; }
        public int IdAlmacen { get; set; }
        public string NombreAlmacen { get; set; } = string.Empty;
        public int Anio { get; set; }
        public int Mes { get; set; }
        public decimal MetaMensual { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? Observaciones { get; set; }
        public decimal? VentaAcumulada { get; set; }
        public decimal? PorcentajeCumplimiento { get; set; }
        public List<MetaMensualDetalleDTO> Detalles { get; set; } = new();
    }
}

