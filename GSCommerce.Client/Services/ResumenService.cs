using GSCommerce.Client.Models;
using System.Net.Http.Json;
using System.Collections.Generic;

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
            var lista = await _http.GetFromJsonAsync<List<AperturaCierreCajaDTO>>($"api/caja/verificar/{idUsuario}/{idAlmacen}");
            return lista != null && lista.Any(a => a.Estado == "A");
        }

        public async Task<ResumenDiarioDTO> ObtenerResumenAsync(int idAlmacen, int idUsuario)
        {
            var response = await _http.GetFromJsonAsync<ResumenDiarioDTO>($"api/ventas/resumen?idAlmacen={idAlmacen}&idUsuario={idUsuario}");
            return response ?? new ResumenDiarioDTO();
        }
    }
}
