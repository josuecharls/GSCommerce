using System.Net.Http.Json;
using GSCommerce.Client.Models.SUNAT;

namespace GSCommerce.Client.Services
{
    public class ComprobantesSunatService
    {
        private readonly HttpClient _http;

        public ComprobantesSunatService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<PendienteSunatDTO>> ObtenerComprobantesAsync(DateTime? desde = null, DateTime? hasta = null)
        {
            var url = "api/comprobantes";
            var q = new List<string>();
            if (desde.HasValue) q.Add($"desde={desde:yyyy-MM-dd}");
            if (hasta.HasValue) q.Add($"hasta={hasta:yyyy-MM-dd}");
            if (q.Count > 0) url += "?" + string.Join("&", q);

            try
            {
                return await _http.GetFromJsonAsync<List<PendienteSunatDTO>>(url) ?? new();
            }
            catch
            {
                return new List<PendienteSunatDTO>();
            }
        }
    }
}