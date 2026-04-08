namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteSeparacionPorMarcasRequest
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int? IdAlmacen { get; set; }
        /// <summary>Claves normalizadas (coinciden con <see cref="MarcaCatalogoDTO.Clave"/>).</summary>
        public List<string> MarcasClave { get; set; } = new();
        public int Top { get; set; } = 50;

        /// <summary>
        /// Si es true, se calcula además la agrupación conjunta talla + color (útil con variantes).
        /// </summary>
        public bool IncluirCombinacionTallaColor { get; set; } = true;
    }
}
