namespace GSCommerce.Client.Models
{
    public class GenerarCredencialesRequest
    {
        public int IdPersonal { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
