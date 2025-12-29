using System.Collections.Generic;

namespace GSCommerce.Client.Models
{
    public class ActualizarDetallesRequest
    {
        public List<DetalleActualizacion> Detalles { get; set; } = new();
    }

    public class DetalleActualizacion
    {
        public int IdMetaMensualDetalle { get; set; }
        public decimal MetaDiaria { get; set; }
    }
}
