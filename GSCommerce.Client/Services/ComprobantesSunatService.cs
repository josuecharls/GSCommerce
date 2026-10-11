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

        public async Task<ComprobantesSunatListResponse> ObtenerComprobantesAsync(
            DateTime? desde = null,
            DateTime? hasta = null,
            int? idAlmacen = null,
            string? estado = null,
            int? tipoDoc = null,
            int page = 1,
            int pageSize = 20)
        {
            var q = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            // Solo yyyy-MM-dd (día calendario) para evitar desfase UTC
            if (desde.HasValue) q.Add($"desde={desde.Value:yyyy-MM-dd}");
            if (hasta.HasValue) q.Add($"hasta={hasta.Value:yyyy-MM-dd}");
            if (idAlmacen.HasValue) q.Add($"idAlmacen={idAlmacen}");
            if (!string.IsNullOrWhiteSpace(estado)) q.Add($"estado={Uri.EscapeDataString(estado)}");
            if (tipoDoc.HasValue) q.Add($"tipoDoc={tipoDoc}");

            var url = "comprobantes?" + string.Join("&", q);

            try
            {
                return await _http.GetFromJsonAsync<ComprobantesSunatListResponse>(url)
                       ?? new ComprobantesSunatListResponse();
            }
            catch
            {
                return new ComprobantesSunatListResponse();
            }
        }

        public class ComprobantesSunatListResponse
        {
            public List<PendienteSunatDTO> Data { get; set; } = new();
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }
    }
}
