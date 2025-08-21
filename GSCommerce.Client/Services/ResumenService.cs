using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using GSCommerce.Client.Models;

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
        public async Task<ResumenDiarioDTO> ObtenerResumenAdminAsync(int? idAlmacen, int? idUsuario)
        {
            var query = new List<string>();
            if (idAlmacen.HasValue)
                query.Add($"idAlmacen={idAlmacen.Value}");
            if (idUsuario.HasValue)
                query.Add($"idUsuario={idUsuario.Value}");
            var url = "api/ventas/resumen-admin" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            var response = await _http.GetFromJsonAsync<ResumenDiarioDTO>(url);
            return response ?? new ResumenDiarioDTO();
        }
    }
}
