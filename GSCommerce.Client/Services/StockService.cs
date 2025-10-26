using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;
using System.Linq;
using System.Net;

namespace GSCommerce.Client.Services
{
    public class StockService
    {
        private readonly HttpClient _httpClient;

        public StockService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResult<StockDTO>> GetStock(int? idAlmacen = null, bool incluirStockCero = false, string? search = null, int filtroBusqueda = 1, int page = 1, int pageSize = 50)
        {
            try
            {
                string url = $"api/stock?page={page}&pageSize={pageSize}&";

                if (idAlmacen.HasValue)
                    url += $"idAlmacen={idAlmacen}&";

                url += $"incluirStockCero={incluirStockCero.ToString().ToLower()}";
                url += $"&filtroBusqueda={filtroBusqueda}";

                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={search}";

                var resultado = await _httpClient.GetFromJsonAsync<PagedResult<StockDTO>>(url);
                return resultado ?? new PagedResult<StockDTO>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("❌ Acceso no autorizado al obtener stock.");
                return new PagedResult<StockDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener stock: {ex.Message}");
                return new PagedResult<StockDTO>();
            }
        }

        public async Task<List<StockDTO>> GetLowStockItems(int? idAlmacen = null, int threshold = 10)
        {
            try
            {
                string url = $"api/stock/low-stock?threshold={threshold}";

                if (idAlmacen.HasValue)
                {
                    url += $"&idAlmacen={idAlmacen.Value}";
                }

                var resultado = await _httpClient.GetFromJsonAsync<List<StockDTO>>(url);
                return resultado ?? new List<StockDTO>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("❌ Acceso no autorizado al obtener el stock crítico.");
                return new List<StockDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener el stock crítico: {ex.Message}");
                return new List<StockDTO>();
            }
        }

        public async Task<int> GetStockDisponible(string codigoArticulo, int idAlmacen)
        {
            var lista = await GetStock(idAlmacen, false, codigoArticulo, 1, 1, 1);
            return lista.Items.FirstOrDefault()?.Stock ?? 0;
        }

        public async Task<List<RepositionAlertDTO>> GetRepositionAlerts(int? idAlmacen = null, int threshold = 10)
        {
            try
            {
                string url = $"api/stock/reposition-alert?threshold={threshold}";
                
                if (idAlmacen.HasValue)
                {
                    url += $"&idAlmacen={idAlmacen.Value}";
                }

                var resultado = await _httpClient.GetFromJsonAsync<List<RepositionAlertDTO>>(url);
                return resultado ?? new List<RepositionAlertDTO>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Acceso no autorizado al obtener alertas de reposición.");
                return new List<RepositionAlertDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener alertas de reposición: {ex.Message}");
                return new List<RepositionAlertDTO>();
            }
        }
    }
}