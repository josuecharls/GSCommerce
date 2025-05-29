namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class DetalleArticuloPorAlmacenDTO
    {
        public string NombreAlmacen { get; set; } = string.Empty;
        public int Ingreso { get; set; }
        public int Venta { get; set; }
        public int Stock { get; set; }
        public decimal PorcentajeVenta { get; set; }
    }
}
