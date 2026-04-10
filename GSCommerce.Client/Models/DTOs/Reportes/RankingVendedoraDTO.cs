namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class RankingVendedoraDTO
    {
        public int IdPersonal { get; set; }

        /// <summary>Almacén del vendedor; la meta diaria se guarda por almacén y fecha.</summary>
        public int? IdAlmacen { get; set; }

        public string Vendedora { get; set; } = string.Empty;
        public decimal TotalVentas { get; set; }
        public bool Activo { get; set; }
        public int TotalClientes { get; set; }
        public int VentasRealizadas { get; set; }
        public int ArticulosVendidos { get; set; }

        /// <summary>Meta guardada: número de ventas (comprobantes) esperadas por día.</summary>
        public int? MetaVentasDiarias { get; set; }

        /// <summary>En vista diaria coincide con <see cref="MetaVentasDiarias"/>; en consultas por rango suele ir null.</summary>
        public int? MetaVentasEnPeriodo { get; set; }

        /// <summary>VentasRealizadas / MetaVentasEnPeriodo × 100. Null si no hay meta.</summary>
        public decimal? PorcentajeVendido { get; set; }
    }
}
