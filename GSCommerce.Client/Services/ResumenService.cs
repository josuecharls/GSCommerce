using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class ResumenService
    {
        private readonly HttpClient _http;

        public ResumenService(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> VerificarAperturaAsync(int idAlmacen, int idUsuario)
        {
            var response = await _http.GetAsync($"api/caja/verificar/{idUsuario}/{idAlmacen}");
            return response.IsSuccessStatusCode;
        }

        public async Task<ResumenDiarioDTO> ObtenerResumenAsync(int idAlmacen, int idUsuario)
        {
            var response = await _http.GetFromJsonAsync<ResumenDiarioDTO>($"api/ventas/resumen?idAlmacen={idAlmacen}&idUsuario={idUsuario}");
            return response ?? new ResumenDiarioDTO();
        }
    }
}
