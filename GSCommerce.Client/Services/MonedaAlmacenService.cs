using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class MonedaAlmacenService
    {
        private readonly HttpClient _httpClient;

        public MonedaAlmacenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<MonedaAlmacenDTO?> ObtenerAsync(int idAlmacen)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MonedaAlmacenDTO>($"api/monedaalmacen/{idAlmacen}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ActualizarAsync(int idAlmacen, string moneda)
        {
            var dto = new MonedaAlmacenDTO { IdAlmacen = idAlmacen, Moneda = moneda };
            var response = await _httpClient.PutAsJsonAsync($"api/monedaalmacen/{idAlmacen}", dto);
            return response.IsSuccessStatusCode;
        }
    }
}