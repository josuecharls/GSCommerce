using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;

        public StockService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<StockDTO>> GetStock(int? idAlmacen = null, bool incluirStockCero = false, string? search = null, int filtroBusqueda = 1)
        {
            try
            {
                string url = "api/stock?";

                if (idAlmacen.HasValue)
                    url += $"idAlmacen={idAlmacen}&";

                url += $"incluirStockCero={incluirStockCero.ToString().ToLower()}";
                url += $"&filtroBusqueda={filtroBusqueda}";

                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={search}";

                var resultado = await _httpClient.GetFromJsonAsync<List<StockDTO>>(url);
                return resultado ?? new List<StockDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener stock: {ex.Message}");
                return new List<StockDTO>();
            }
        }
    }
}