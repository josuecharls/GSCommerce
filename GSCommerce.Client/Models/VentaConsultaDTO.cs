using System;
using System.Collections.Generic;

namespace GSCommerce.Client.Models
{
    public class VentaConsultaDTO
    {
        public int IdComprobante { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Numero { get; set; }
        public DateTime Fecha { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string EstadoSunat { get; set; } = string.Empty;
        public string? DescripcionSunat { get; set; }
        public string? GeneroNC { get; set; }
        public string NumeroNotaCredito => string.IsNullOrWhiteSpace(GeneroNC) ? "" : GeneroNC;
        public List<DetallePagoDTO> Pagos { get; set; } = new();

    }
}
