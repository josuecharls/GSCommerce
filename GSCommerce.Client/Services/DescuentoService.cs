using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class DescuentoService
    {
        private readonly HttpClient _httpClient;
        public DescuentoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<DescuentoDTO>> ObtenerPorAlmacen(int idAlmacen)
        {
            var resp = await _httpClient.GetFromJsonAsync<List<DescuentoDTO>>($"api/descuentos/{idAlmacen}");
            return resp ?? new();
        }

        public async Task<double> ObtenerPorArticulo(int idAlmacen, string idArticulo)
        {
            try
            {
                var value = await _httpClient.GetFromJsonAsync<double>($"api/descuentos/{idAlmacen}/articulo/{idArticulo}");
                return value;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<DescuentoDTO?> CrearAsync(DescuentoDTO dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/descuentos", dto);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<DescuentoDTO>();
            return null;
        }

        public async Task ActualizarAsync(int id, DescuentoDTO dto)
        {
            await _httpClient.PutAsJsonAsync($"api/descuentos/{id}", dto);
        }

        public async Task EliminarAsync(int id)
        {
            await _httpClient.DeleteAsync($"api/descuentos/{id}");
        }
    }
}