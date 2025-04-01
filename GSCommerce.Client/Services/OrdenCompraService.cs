using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class OrdenCompraService
    {
        private readonly HttpClient _httpClient;

        public OrdenCompraService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OrdenCompraDTO?> ObtenerOrdenPorId(int id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<OrdenCompraDTO>($"api/ordenes-compra/{id}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener orden de compra: {ex.Message}");
                return null;
            }
        }
    }
}
