using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
                string url = $"stock?page={page}&pageSize={pageSize}&";

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
                string url = $"stock/low-stock?threshold={threshold}";

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
            try
            {
                var url =
                    $"stock/cantidad-total?codigoArticulo={Uri.EscapeDataString(codigoArticulo.Trim())}&idAlmacen={idAlmacen}";
                var r = await _httpClient.GetFromJsonAsync<CantidadTotalResponse>(url);
                return r?.CantidadTotal ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cantidad-total stock, fallback paginado: {ex.Message}");
                var lista = await GetStock(idAlmacen, true, codigoArticulo, 1, 1, 500);
                return lista.Items
                    .Where(i => string.Equals(i.IdArticulo, codigoArticulo.Trim(), StringComparison.OrdinalIgnoreCase))
                    .Sum(i => i.Stock);
            }
        }

        /// <summary>
        /// Cantidad en la fila exacta (almacén + artículo + variante) que valida el registro de venta.
        /// </summary>
        public async Task<int> GetStockCantidadFila(string codigoArticulo, int idAlmacen, int? idVariante)
        {
            try
            {
                var baseUrl =
                    $"stock/cantidad-fila?codigoArticulo={Uri.EscapeDataString(codigoArticulo.Trim())}&idAlmacen={idAlmacen}";
                var url = idVariante is > 0
                    ? $"{baseUrl}&idVariante={idVariante.Value}"
                    : baseUrl;
                var r = await _httpClient.GetFromJsonAsync<CantidadFilaResponse>(url);
                return r?.Cantidad ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cantidad-fila stock: {ex.Message}");
                // Sin IdVariante en StockDTO no podemos filtrar por variante en el listado; evitamos sobreestimar.
                if (idVariante is > 0)
                    return 0;
                return await GetStockDisponible(codigoArticulo, idAlmacen);
            }
        }

        private sealed class CantidadTotalResponse
        {
            [JsonPropertyName("cantidadTotal")]
            public int CantidadTotal { get; set; }
        }

        private sealed class CantidadFilaResponse
        {
            [JsonPropertyName("cantidad")]
            public int Cantidad { get; set; }
        }

        public async Task<int> GetStockDisponibleParaTransferencia(string codigoArticulo, int idAlmacen, int? idMovimientoExcluir = null)
        {
            try
            {
                var url = $"stock/disponible-para-transferencia?codigoArticulo={Uri.EscapeDataString(codigoArticulo)}&idAlmacen={idAlmacen}";
                if (idMovimientoExcluir.HasValue)
                {
                    url += $"&idMovimientoExcluir={idMovimientoExcluir.Value}";
                }

                var response = await _httpClient.GetFromJsonAsync<StockDisponibleResponse>(url);
                return response?.StockDisponible ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener stock disponible para transferencia: {ex.Message}");
                // Fallback al método anterior si hay error
                return await GetStockDisponible(codigoArticulo, idAlmacen);
            }
        }

        private class StockDisponibleResponse
        {
            public int StockDisponible { get; set; }
        }

        public async Task<List<RepositionAlertDTO>> GetRepositionAlerts(int? idAlmacen = null, int threshold = 10)
        {
            try
            {
                string url = $"stock/reposition-alert?threshold={threshold}";
                
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