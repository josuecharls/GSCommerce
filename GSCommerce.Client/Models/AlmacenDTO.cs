namespace GSCommerce.Client.Models
{
    public class AlmacenDTO
    {
        public int IdAlmacen { get; set; }

        public string Nombre { get; set; } = null!;

        public bool EsTienda { get; set; }

        public string Direccion { get; set; } = null!;

        public string? Dpd { get; set; }

        public string? Telefono { get; set; }

        public string? Celular { get; set; }

        public string? RazonSocial { get; set; }

        public string? Ruc { get; set; }

        public bool Estado { get; set; }

        public string? Ubigeo { get; set; }

        public string? Certificado { get; set; }

        public string? PasswordCertificado { get; set; }

        public string? Abreviacion { get; set; }

        public string? UsuarioSol { get; set; }

        public string? ClaveSol { get; set; }

        public bool AfectoIgv { get; set; }
    }
}
