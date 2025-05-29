using GSCommerce.Client.Models;
using GSCommerce.Client.Models.Series;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class SerieCorrelativoService
    {
        private readonly HttpClient _httpClient;
        public SerieCorrelativoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener series por almacén
        public async Task<List<SerieCorrelativoDTO>> ObtenerPorAlmacenAsync(int idAlmacen)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<SerieCorrelativoDTO>>(
                    $"api/seriescorrelativos/almacen/{idAlmacen}");
                return response ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener series: {ex.Message}");
                return new();
            }
        }
        public async Task<string> ObtenerSeriePorTipoYAlmacen(string tipoDoc, int idAlmacen)
        {
            try
            {
                var url = $"api/seriescorrelativos/buscar?tipoDoc={tipoDoc}&idAlmacen={idAlmacen}";
                var response = await _httpClient.GetStringAsync(url);
                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ No se encontró la serie: {ex.Message}");
                return string.Empty;
            }
        }

        // Crear una nueva serie
        public async Task<bool> CrearSerieAsync(SerieCorrelativoDTO dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/seriescorrelativos", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear serie: {ex.Message}");
                return false;
            }
        }

        // Actualizar correlativo o estado
        public async Task<bool> ActualizarSerieAsync(SerieCorrelativoDTO dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/seriescorrelativos/{dto.IdSerieCorrelativo}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar serie: {ex.Message}");
                return false;
            }
        }

        // Desactivar una serie
        public async Task<bool> EliminarSerieAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/seriescorrelativos/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar serie: {ex.Message}");
                return false;
            }
        }

        // Incrementar correlativo
        public async Task<bool> IncrementarCorrelativo(string serie, int idAlmacen, int idTipoDocumentoVenta)
        {
            try
            {
                // Buscar la serie por almacén y tipo documento
                var series = await ObtenerPorAlmacenAsync(idAlmacen);
                var serieObj = series.FirstOrDefault(s => s.Serie == serie && s.IdTipoDocumentoVenta == idTipoDocumentoVenta);

                if (serieObj == null) return false;

                serieObj.Correlativo += 1;

                var response = await _httpClient.PutAsJsonAsync($"api/seriescorrelativos/{serieObj.IdSerieCorrelativo}", serieObj);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al incrementar correlativo: {ex.Message}");
                return false;
            }
        }

        public async Task<List<VSeriesXalmacen1>> ObtenerSeriesDisponibles(int idAlmacen)
        {
            return await _httpClient.GetFromJsonAsync<List<VSeriesXalmacen1>>(
                $"api/seriescorrelativos/disponibles/{idAlmacen}") ?? new();
        }

        public async Task<List<VSeriesXcajero1>> ObtenerSeriesAsignadas(int idUsuario, int idAlmacen)
        {
            return await _httpClient.GetFromJsonAsync<List<VSeriesXcajero1>>(
                $"api/seriescorrelativos/asignadas?usuario={idUsuario}&almacen={idAlmacen}") ?? new();
        }

        public async Task<bool> AsignarSeriesACajero(int idUsuario, int idAlmacen, List<VSeriesXcajero1> series)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/seriescorrelativos/asignar", new
            {
                IdUsuario = idUsuario,
                IdAlmacen = idAlmacen,
                Series = series.Select(s => s.IdSerieCorrelativo).ToList()
            });

            return response.IsSuccessStatusCode;
        }
    }
}
