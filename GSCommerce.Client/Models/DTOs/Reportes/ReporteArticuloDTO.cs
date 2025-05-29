namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteArticuloDTO
    {
        public int IdArticulo { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public List<DetalleArticuloPorAlmacenDTO> DetallePorAlmacen { get; set; } = new();
        public List<ResumenVentasMensualDTO> TotalVentasMensual { get; set; } = new();
    }

}
