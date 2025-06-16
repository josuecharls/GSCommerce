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
            int? idAlmacen = await _auth.GetUserAlmacenId();
            if (idAlmacen == null)
                return new();
            string url = $"api/ventas/pendientes-sunat?fecha={fecha:yyyy-MM-dd}&idAlmacen={idAlmacen}";
            var response = await _http.GetFromJsonAsync<List<PendienteSunatDTO>>(url);
            return response ?? new();
        }

        public async Task<string> GenerarResumenAsync(DateTime fecha)
        {
            int? idAlmacen = await _auth.GetUserAlmacenId();
            if (idAlmacen == null)
                return "Almac\u00e9n no encontrado";

            var response = await _http.PostAsync($"api/ventas/generar-resumen?fecha={fecha:yyyy-MM-dd}&idAlmacen={idAlmacen}", null);
            return await response.Content.ReadAsStringAsync();
        }
    }
}