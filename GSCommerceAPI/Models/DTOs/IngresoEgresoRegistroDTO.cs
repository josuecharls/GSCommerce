using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models.DTOs
{
    public class IngresoEgresoRegistroDTO
    {
        public int IdUsuario { get; set; }
        public int IdAlmacen { get; set; }
        public string Naturaleza { get; set; } = string.Empty; // Ingreso o Egreso
        public string Tipo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Glosa { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public int? IdProveedor { get; set; }
        public int? IdAlmacenDestino { get; set; }
        public int? IdCajeroDestino { get; set; }
        public int? IdAlmacenGasto { get; set; }
        public List<IngresoEgresoDetalleDTO> Detalles { get; set; } = new();
    }
}