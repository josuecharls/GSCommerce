namespace GSCommerce.Client.Models
{
    public class VentaCabeceraDTO
    {
        public int IdComprobante { get; set; }
        public string TipoDocumento { get; set; } = string.Empty; // BOLETA, FACTURA, TICKET
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime FechaEmision { get; set; } = DateTime.Now;
        public string DocumentoCliente { get; set; } = "99999999";
        public string NombreCliente { get; set; } = "CLIENTE VARIOS";
        public string DireccionCliente { get; set; } = "-";
        public int IdVendedor { get; set; }
        public string NombreVendedor { get; set; } = string.Empty;
        public int IdAlmacen { get; set; }
        public int? IdCajero { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal Redondeo { get; set; }
        public decimal APagar { get; set; }
        public decimal Vuelto { get; set; }
        public string? CodigoVerificacion { get; set; }
        public string? RucEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
        public bool PendienteSunat { get; set; } = true;
        public string? NombreCajero { get; set; }
        public string? DireccionEmisor { get; set; }
        public List<VentaDetalleDTO> Detalles { get; set; } = new();
        public List<DetallePagoDTO> Pagos { get; set; } = new();
    }
}
