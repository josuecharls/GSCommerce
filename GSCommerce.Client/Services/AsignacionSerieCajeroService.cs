using GSCommerce.Client.Models;
using GSCommerce.Client.Models.Series;
using System.Net.Http;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class AsignacionSerieCajeroService
    {
        private readonly HttpClient _http;

        public AsignacionSerieCajeroService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<VSeriesXalmacen1>> ObtenerSeriesDisponibles(int idAlmacen)
        {
            return await _http.GetFromJsonAsync<List<VSeriesXalmacen1>>($"api/asignacionseriecajero/disponibles?idAlmacen={idAlmacen}") ?? new();
        }

        public async Task<List<VSeriesXcajero1>> ObtenerSeriesAsignadas(int idUsuario, int idAlmacen)
        {
            return await _http.GetFromJsonAsync<List<VSeriesXcajero1>>(
                $"api/asignacionseriecajero/asignadas?idUsuario={idUsuario}&idAlmacen={idAlmacen}") ?? new();
        }

        public async Task<bool> GuardarAsignacion(AsignacionSerieCajeroDTO dto)
        {
            var resp = await _http.PostAsJsonAsync("api/asignacionseriecajero/asigna", dto);
            return resp.IsSuccessStatusCode;
        }
        public async Task<bool> AsignarSeriesACajero(int idUsuario, int idAlmacen, List<VSeriesXcajero1> series)
        {
            var response = await _http.PostAsJsonAsync($"api/asignacionseriecajero/guardar", new
            {
                IdUsuario = idUsuario,
                IdAlmacen = idAlmacen,
                IdSeries = series.Select(s => s.IdSerieCorrelativo).ToList()
            });

            return response.IsSuccessStatusCode;
        }
    }

}
