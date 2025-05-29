using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class KardexService
    {
        private readonly HttpClient _http;

        public KardexService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<KardexDTO>> ObtenerKardex(
            string? articulo = null,
            int? idAlmacen = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(articulo))
                query.Add($"articulo={Uri.EscapeDataString(articulo)}");

            if (idAlmacen.HasValue)
                query.Add($"idAlmacen={idAlmacen}");

            if (desde.HasValue)
                query.Add($"desde={desde.Value:yyyy-MM-dd}");

            if (hasta.HasValue)
                query.Add($"hasta={hasta.Value:yyyy-MM-dd}");

            var queryString = query.Count > 0 ? "?" + string.Join("&", query) : "";

            return await _http.GetFromJsonAsync<List<KardexDTO>>($"api/kardex{queryString}")
                   ?? new List<KardexDTO>();
        }
    }
}
