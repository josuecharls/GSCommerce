namespace GSCommerce.Client.Models
{
    public class DetallePagoDTO
    {
        public int IdDetallePagoVenta { get; set; }

        public int IdComprobante { get; set; }

        public int IdTipoPagoVenta { get; set; }

        public decimal Soles { get; set; }

        public decimal Dolares { get; set; }

        public string? Datos { get; set; }

        public decimal? Vuelto { get; set; }

        public decimal? PorcentajeTarjetaSoles { get; set; }

        public decimal? PorcentajeTarjetaDolares { get; set; }

        public string FormaPago { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public string? CodigoVerificacion { get; set; }

        public string Tipo { get; set; } = string.Empty;

    }
}
