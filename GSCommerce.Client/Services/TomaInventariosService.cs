using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class TomaInventariosService
    {
        private readonly HttpClient _httpClient;

        public TomaInventariosService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TomaInventarioDTO>> GetTomasAsync(int? anio = null, int? idAlmacen = null)
        {
            string url = "api/toma-inventarios?";
            if (anio.HasValue)
                url += $"anio={anio.Value}&";
            if (idAlmacen.HasValue)
                url += $"idAlmacen={idAlmacen.Value}&";
            var data = await _httpClient.GetFromJsonAsync<List<TomaInventarioDTO>>(url);
            return data ?? new();
        }

        public async Task<TomaInventarioDTO?> CrearAsync(TomaInventarioDTO dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/toma-inventarios", dto);
            if (!response.IsSuccessStatusCode)
                return null;
            return await response.Content.ReadFromJsonAsync<TomaInventarioDTO>();
        }

        public async Task<List<TomaInventarioDetalleDTO>> GetDetallesAsync(int idToma)
        {
            var data = await _httpClient.GetFromJsonAsync<List<TomaInventarioDetalleDTO>>($"api/toma-inventarios/{idToma}/detalles");
            return data ?? new();
        }

        public async Task<bool> AgregarDetalleAsync(int idToma, TomaInventarioDetalleDTO dto)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/toma-inventarios/{idToma}/detalles", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> TerminarAsync(int idToma)
        {
            var response = await _httpClient.PutAsync($"api/toma-inventarios/{idToma}/terminar", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AnularAsync(int idToma)
        {
            var response = await _httpClient.PutAsync($"api/toma-inventarios/{idToma}/anular", null);
            return response.IsSuccessStatusCode;
        }
    }
}