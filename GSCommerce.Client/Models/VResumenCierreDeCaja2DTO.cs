﻿namespace GSCommerce.Client.Models
{
    public class VResumenCierreDeCaja2DTO
    {
        public int IdResumen { get; set; }

        public int IdUsuario { get; set; }

        public int IdAlmacen { get; set; }

        public DateOnly? Fecha { get; set; }

        public int IdGrupo { get; set; }

        public string Grupo { get; set; } = null!;

        public string? Detalle { get; set; }

        public decimal Monto { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}
