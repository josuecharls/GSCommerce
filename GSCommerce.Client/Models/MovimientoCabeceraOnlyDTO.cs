namespace GSCommerce.Client.Models
{
    public class MovimientoCabeceraOnlyDTO
    {
        public int IdMovimiento { get; set; }
        public int IdAlmacen { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } // SQL espera date → OK con DateTime
        public string Descripcion { get; set; } = string.Empty;

        public int? IdProveedor { get; set; }
        public int? IdAlmacenDestinoOrigen { get; set; }
        public int? IdOc { get; set; }

        public int IdUsuario { get; set; }
        public DateTime FechaHoraRegistro { get; set; } = DateTime.Now;

        public int? IdGuiaRemision { get; set; }
        public int? IdUsuarioConfirma { get; set; }
        public DateTime? FechaHoraConfirma { get; set; }
        public string Estado { get; set; } = "E";
    }
}
