namespace GSCommerce.Client.Models.DTOs.Reportes
{
    /// <summary>
    /// Tallas y colores agregados solo para líneas cuya marca está en el filtro (multimarca).
    /// </summary>
    public class ReporteSeparacionDTO
    {
        public List<MetricaSeparacionDTO> VentasPorTalla { get; set; } = new();
        public List<MetricaSeparacionDTO> VentasPorColor { get; set; } = new();
        /// <summary>Par talla — color (solo si se solicitó en el request).</summary>
        public List<MetricaSeparacionDTO> VentasPorCombinacionTallaColor { get; set; } = new();
    }
}
