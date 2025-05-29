namespace GSCommerce.Client.Models
{
    public class VCierreVentaDiaria1DTO
    {
        public int IdTipoPagoVenta { get; set; }

        public string Descripcion { get; set; } = null!;

        public string Datos { get; set; } = null!;

        public string Serie { get; set; } = null!;

        public int Numero { get; set; }

        public DateTime Fecha { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = null!;

        public decimal Soles { get; set; }

        public decimal Dolares { get; set; }

        public decimal? Redondeo { get; set; }

        public decimal? Vuelto { get; set; }

        public int IdCajero { get; set; }

        public int IdAlmacen { get; set; }
    }
}
