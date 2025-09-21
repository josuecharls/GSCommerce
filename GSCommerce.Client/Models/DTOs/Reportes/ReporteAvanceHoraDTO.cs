using System;
using System.Collections.Generic;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class ReporteAvanceHoraDTO
    {
        public DateTime HoraInicio { get; set; }
        public DateTime HoraFin { get; set; }
        public decimal TotalVentas { get; set; }
        public decimal TotalVentasDia { get; set; }
        public int Tickets { get; set; }
        public int TicketsDia { get; set; }
        public List<ReporteAvanceHoraDetalleDTO> DetalleHoras { get; set; } = new();
    }

    public class ReporteAvanceHoraDetalleDTO
    {
        public DateTime HoraInicio { get; set; }
        public DateTime HoraFin { get; set; }
        public decimal TotalHora { get; set; }
        public decimal TotalAcumulado { get; set; }
        public int TicketsHora { get; set; }
        public int TicketsAcumulados { get; set; }
    }
}