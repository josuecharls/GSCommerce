namespace GSCommerce.Client.Models
{
    public class UsuarioDTO
    {
        public int IdUsuario { get; set; }

        public string Nombre { get; set; } = null!;

        public int IdPersonal { get; set; }

        public bool Estado { get; set; }

        public byte[]? Clave { get; set; }
    }
}
