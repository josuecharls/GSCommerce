namespace GSCommerce.Client.Models
{
    public class OrdenCompraDTO
    {
        public int IdOc { get; set; }
        public int IdProveedor { get; set; }
        public string NumeroOc { get; set; } = string.Empty;
        public DateTime FechaOc { get; set; }
        public string RucProveedor { get; set; } = string.Empty;
        public string NombreProveedor { get; set; } = string.Empty;
        public string DireccionProveedor { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal TipoCambio { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public bool SinIgv { get; set; }
        public DateTime FechaEntrega { get; set; }
        public string Atencion { get; set; } = string.Empty;
        public string Glosa { get; set; } = string.Empty;
        public decimal ImporteSubTotal { get; set; }
        public decimal ImporteIgv { get; set; }
        public decimal ImporteTotal { get; set; }
        public bool EstadoEmision { get; set; }
        public string EstadoAtencion { get; set; } = string.Empty;

        public List<OrdenCompraDetalleDTO> Detalles { get; set; } = new();
    }
}
