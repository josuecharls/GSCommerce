using System.Collections.Generic;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class VentasFamiliaRequestDTO
    {
        public List<int> Anios { get; set; } = new();
        public List<int> Meses { get; set; } = new();
        public List<string> Familias { get; set; } = new();
        public List<string> Marcas { get; set; } = new();
    }

    public class VentaFamiliaMensualDTO
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string AlmacenNombre { get; set; } = string.Empty;
        public int CantidadVendida { get; set; }
    }

    public class ReporteVentasFamiliaResponseDTO
    {
        public List<VentaFamiliaMensualDTO> Resultados { get; set; } = new();
    }
}
