using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class IgvAlmacenService
    {
        private readonly HttpClient _httpClient;

        public IgvAlmacenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IgvAlmacenDTO?> ObtenerAsync(int idAlmacen)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IgvAlmacenDTO>($"api/igvalmacen/{idAlmacen}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ActualizarAsync(int idAlmacen, decimal igv)
        {
            var dto = new IgvAlmacenDTO { IdAlmacen = idAlmacen, Igv = igv };
            var response = await _httpClient.PutAsJsonAsync($"api/igvalmacen/{idAlmacen}", dto);
            return response.IsSuccessStatusCode;
        }
    }
}