using System;
using System.Collections.Generic;
using System.Linq;

namespace GSCommerce.Client.Models.DTOs.Reportes
{
    public class PagoTarjetaOnlineResumenDTO
    {
        public int IdTipoPagoVenta { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal TotalSoles { get; set; }
        public decimal TotalDolares { get; set; }
        public int CantidadOperaciones { get; set; }
        public decimal Total => TotalSoles + TotalDolares;
    }

    public class PagoTarjetaOnlineDetalleDTO
    {
        public int IdComprobante { get; set; }
        public DateTime Fecha { get; set; }
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public int IdTipoDocumento { get; set; }
        public string Metodo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public decimal Soles { get; set; }
        public decimal Dolares { get; set; }
        public string? CodigoOperacion { get; set; }
        public int IdAlmacen { get; set; }
        public string? Almacen { get; set; }
        public int IdCajero { get; set; }
        public string? Cajero { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Total => Soles + Dolares;
        public string Comprobante => $"{Serie}-{Numero:D8}";
        public bool EstaAnulado => string.Equals(Estado, "A", StringComparison.OrdinalIgnoreCase);
    }

    public class ReportePagosTarjetaOnlineResponseDTO
    {
        public List<PagoTarjetaOnlineResumenDTO> Totales { get; set; } = new();
        public List<PagoTarjetaOnlineDetalleDTO> Detalles { get; set; } = new();
    }
}