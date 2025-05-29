using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class UsuarioService
    {
        private readonly HttpClient _httpClient;

        public UsuarioService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<UsuarioDTO>> ObtenerCajerosAsync(int idAlmacen)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UsuarioDTO>>($"api/usuarios/cajeros/{idAlmacen}");
                return response ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener cajeros: {ex.Message}");
                return new();
            }
        }
    }
}
