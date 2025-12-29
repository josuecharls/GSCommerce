using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class DireccionRucService
    {
        private readonly HttpClient _http;

        public DireccionRucService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<DireccionRucDTO>> ObtenerDireccionesPorRuc(string ruc)
        {
            try
            {
                var response = await _http.GetAsync($"api/DireccionRuc/{ruc}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<DireccionRucDTO>>() ?? new List<DireccionRucDTO>();
                }
                return new List<DireccionRucDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo direcciones: {ex.Message}");
                return new List<DireccionRucDTO>();
            }
        }

        public async Task<DireccionRucDTO?> CrearDireccion(DireccionRucDTO direccion)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/DireccionRuc", direccion);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DireccionRucDTO>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creando dirección: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> ActualizarDireccion(DireccionRucDTO direccion)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"api/DireccionRuc/{direccion.IdDireccionRuc}", direccion);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando dirección: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EliminarDireccion(int id)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/DireccionRuc/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando dirección: {ex.Message}");
                return false;
            }
        }
    }
}
