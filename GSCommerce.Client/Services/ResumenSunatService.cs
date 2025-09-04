using System.Net.Http.Json;
using GSCommerce.Client.Models.SUNAT;

namespace GSCommerce.Client.Services
{
    public class ResumenSunatService
    {
        private readonly HttpClient _http;
        private readonly AuthService _auth;

        public ResumenSunatService(HttpClient http, AuthService auth)
        {
            _http = http;
            _auth = auth;
        }

        public async Task<List<PendienteSunatDTO>> ObtenerPendientesAsync(DateTime fecha)
        {
            var cargo = await _auth.GetUserCargo() ?? string.Empty;
            var url = $"api/ventas/pendientes-sunat?fecha={fecha:yyyy-MM-dd}";
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            {
                var idAlmacen = await _auth.GetUserAlmacenId();
                if (idAlmacen != null) url += $"&idAlmacen={idAlmacen}";
            }

            try
            {
                return await _http.GetFromJsonAsync<List<PendienteSunatDTO>>(url) ?? new();
            }
            catch (HttpRequestException ex)
            {
               // Console.WriteLine($"[ResumenSunatService] {ex.Message}");
                return new(); // evita el “Unhandled exception rendering component”
            }
        }

        public async Task<string> GenerarResumenAsync(DateTime fecha)
        {
            var idAlmacen = await _auth.GetUserAlmacenId();
            if (idAlmacen == null) return "Almacén no encontrado";

            var resp = await _http.PostAsync($"api/ventas/generar-resumen?fecha={fecha:yyyy-MM-dd}&idAlmacen={idAlmacen}", null);
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<string?> ObtenerXmlAsync(int idComprobante)
        {
            try { return await _http.GetStringAsync($"api/ventas/xml/{idComprobante}"); }
            catch { return null; }
        }

        public async Task<bool> ActualizarXmlAsync(int idComprobante, string xml)
        {
            var payload = new { xml };
            var resp = await _http.PutAsJsonAsync($"api/ventas/xml/{idComprobante}", payload);
            return resp.IsSuccessStatusCode;
        }

        public async Task<string> ReintentarAsync(int idComprobante, string modo)
        {
            var resp = await _http.PostAsync($"api/ventas/reintentar/{idComprobante}?modo={modo}", null);
            return await resp.Content.ReadAsStringAsync();
        }
    }
}