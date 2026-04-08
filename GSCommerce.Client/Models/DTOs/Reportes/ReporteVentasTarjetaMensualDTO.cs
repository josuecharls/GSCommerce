using System.Collections.Generic;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    /// <summary>
    /// Totales por método de pago para un mes del año (misma forma que pagos-tarjeta-online).
    /// </summary>
    public class VentaTarjetaMesTotalesDTO
    {
        public int Month { get; set; }
        public List<PagoTarjetaOnlineResumenDTO> Totales { get; set; } = new();
    }

    public class ReporteVentasTarjetaMensualResponseDTO
    {
        public int Year { get; set; }
        public List<VentaTarjetaMesTotalesDTO> Meses { get; set; } = new();
        public List<PagoTarjetaOnlineResumenDTO> TotalesAnio { get; set; } = new();
    }
}
