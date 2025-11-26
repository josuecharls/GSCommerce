using System.Collections.Generic;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class CalculoDistribucionRequest
    {
        public List<FamiliaDistribucionRequest> Familias { get; set; } = new();
    }

    public class FamiliaDistribucionRequest
    {
        public string Familia { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    public class DistribucionPorFamiliaDTO
    {
        public string Familia { get; set; } = string.Empty;
        public string Linea { get; set; } = string.Empty;
        public int StockDisponible { get; set; }
        public int CantidadSolicitada { get; set; }
        public List<DistribucionAlmacenDTO> Destinos { get; set; } = new();
    }

    public class DistribucionAlmacenDTO
    {
        public int IdAlmacen { get; set; }
        public string Almacen { get; set; } = string.Empty;
        public double Porcentaje { get; set; }
        public int Cantidad { get; set; }
    }
}

