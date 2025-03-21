namespace GSCommerce.Client.Models
{
    public class ArticuloDTO
    {
        public string IdArticulo { get; set; } = null!;

        public string Descripcion { get; set; } = null!;

        public string DescripcionCorta { get; set; } = null!;

        public string Familia { get; set; } = null!;

        public string Linea { get; set; } = null!;

        public string Marca { get; set; } = null!;

        public string Material { get; set; } = null!;

        public string Modelo { get; set; } = null!;

        public string Color { get; set; } = null!;

        public string Detalle { get; set; } = null!;

        public string Talla { get; set; } = null!;

        public int IdProveedor { get; set; }

        public string UnidadAlmacen { get; set; } = null!;

        public string MonedaCosteo { get; set; } = null!;

        public decimal PrecioCompra { get; set; }

        public decimal PrecioVenta { get; set; }

        public DateOnly FechaRegistro { get; set; }

        public string? Foto { get; set; }

        public byte[]? CodigoBarra { get; set; }

        public bool Estado { get; set; }

        public string? Estacion { get; set; }
    }
}
