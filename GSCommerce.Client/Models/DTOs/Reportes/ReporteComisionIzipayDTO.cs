using System.Collections.Generic;
using System.Linq;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ComisionIzipayFilaDTO
    {
        public string Categoria { get; set; } = string.Empty;
        public int Movimientos { get; set; }
        public decimal Bruto { get; set; }
        public decimal Comision { get; set; }
        public decimal Neto => Bruto - Comision;
    }

    public class ComisionIzipayGrupoDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public List<ComisionIzipayFilaDTO> Filas { get; set; } = new();

        public int TotalMovimientos => Filas.Sum(f => f.Movimientos);
        public decimal TotalBruto => Filas.Sum(f => f.Bruto);
        public decimal TotalComision => Filas.Sum(f => f.Comision);
        public decimal TotalNeto => Filas.Sum(f => f.Neto);
    }

    public class ReporteComisionIzipayResponseDTO
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public ComisionIzipayGrupoDTO Total { get; set; } = new();
        public List<ComisionIzipayGrupoDTO> PorRazonSocial { get; set; } = new();
        public List<ComisionIzipayGrupoDTO> PorAlmacen { get; set; } = new();
    }

    public class ReporteComisionIzipayAnualResponseDTO
    {
        public int AnioDesde { get; set; }
        public int AnioHasta { get; set; }
        public List<ComisionIzipayGrupoDTO> PorAnio { get; set; } = new();
        public ComisionIzipayGrupoDTO Total { get; set; } = new();
    }
}
