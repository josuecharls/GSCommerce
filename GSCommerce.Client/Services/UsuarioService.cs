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

        public async Task<UsuarioDTO?> GetUsuarioById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UsuarioDTO>($"api/usuarios/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
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

        public async Task<List<UsuarioDTO>> ObjetenerCajerosConUserAsync(int idAlmacen)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UsuarioDTO>>($"api/usuarios/cajeros2/{idAlmacen}");
                return response ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener cajeros con usuario: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> GenerarCredencialesAsync(GenerarCredencialesRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/usuarios/generar-credenciales", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generando credenciales: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UsuarioDTO>> ObtenerAdministradoresAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<UsuarioDTO>>("api/usuarios/administradores");
                return response ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener administradores: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> CambiarPasswordAsync(CambiarPasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/usuarios/cambiar-password", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al cambiar contraseña: {ex.Message}");
                return false;
            }
        }
    }
}
