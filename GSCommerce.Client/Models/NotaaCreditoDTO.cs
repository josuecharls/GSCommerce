namespace GSCommerce.Client.Models
{
    public class NotaaCreditoDTO
    {
        public int IdNc { get; set; }

        public int IdTipoDocumento { get; set; }

        public string Almacen { get; set; } = null!;

        public string Serie { get; set; } = null!;

        public int Numero { get; set; }

        public DateOnly? Fecha { get; set; }

        public string Referencia { get; set; } = null!;

        public string IdMotivo { get; set; } = null!;

        public int IdCliente { get; set; }

        public string Dniruc { get; set; } = null!;

        public string Nombre { get; set; } = null!;

        public decimal Total { get; set; }

        public int IdUsuario { get; set; }

        public int IdAlmacen { get; set; }

        public string Estado { get; set; } = null!;

        public bool? Empleada { get; set; }

        public string? Comprobante { get; set; }
        public string NumeroNotaCredito => $"{Serie}-{Numero.ToString().PadLeft(8, '0')}";
    }
}
