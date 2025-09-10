namespace GSCommerce.Client.Models
{
    public class CambiarPasswordRequest
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNuevo { get; set; } = string.Empty;
    }
}
