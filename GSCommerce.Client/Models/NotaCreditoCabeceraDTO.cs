namespace GSCommerce.Client.Models
{
    public class NotaCreditoCabeceraDTO
    {
        public int IdTipoDocumento { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string IdMotivo { get; set; } = "01";
        public int IdCliente { get; set; }
        public string Dniruc { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Igv { get; set; }
        public decimal Total { get; set; }
        public decimal Redondeo { get; set; }
        public decimal AFavor { get; set; }
        public int IdUsuario { get; set; }
        public int IdAlmacen { get; set; }
        public string Estado { get; set; } = "E";
    }
}