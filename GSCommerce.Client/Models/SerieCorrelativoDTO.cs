namespace GSCommerce.Client.Models
{
    public class SerieCorrelativoDTO
    {
        public int IdSerieCorrelativo { get; set; }
        public int IdAlmacen { get; set; }
        public int IdTipoDocumentoVenta { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Correlativo { get; set; }
        public bool Estado { get; set; }

        // Extra opcional para mostrar
        public string? NombreAlmacen { get; set; }
        public string? NombreTipoDocumento { get; set; }
    }
}
