
    namespace GSCommerce.Client.Models
    {
    public class VentaDTO
    {
        public int IdTipoDocumento { get; set; }
        public string Serie { get; set; } = "";
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public int? IdCliente { get; set; }
        public string? Dniruc { get; set; }
        public string Nombre { get; set; } = "";
        public string? Direccion { get; set; }
        public decimal TipoCambio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal? Redondeo { get; set; }
        public decimal? Apagar { get; set; }
        public decimal AFavor { get; set; }
        public int IdVendedor { get; set; }
        public int IdCajero { get; set; }
        public int IdAlmacen { get; set; }
        public int IdArticulo { get; set; }
        public string? Descripcion { get; set; }
        public List<VentaDetalleDTO> Detalles { get; set; } = new();
        public string? RucEmisor { get; set; }
        public string? RazonSocialEmisor { get; set; }
        public string? DireccionEmisor { get; set; }
        public string? NombreCajero { get; set; }
        public string? NombreVendedor { get; set; }
        public decimal Vuelto { get; set; }
        public List<DetallePagoDTO> Pagos { get; set; } = new();
    }
}