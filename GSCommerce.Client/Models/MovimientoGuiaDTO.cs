using System;
using System.Collections.Generic;

namespace GSCommerce.Client.Models
{
    public class MovimientoGuiaDTO
    {
        // Cabecera
        public int IdMovimiento { get; set; }
        public int IdAlmacen { get; set; }
        public string Tipo { get; set; } = string.Empty; // I, E o T
        public string Motivo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Descripcion { get; set; } = string.Empty;
        public int? IdProveedor { get; set; }
        public int? IdAlmacenDestinoOrigen { get; set; }
        public int? IdOc { get; set; }
        public int IdUsuario { get; set; }
        public string Estado { get; set; } = "E"; // E = Emitido

        // Detalles
        public List<MovimientoDetalleDTO> Detalles { get; set; } = new();

        public int? IdUsuarioConfirma { get; set; }
        public DateTime? FechaHoraConfirma { get; set; }
    }
}