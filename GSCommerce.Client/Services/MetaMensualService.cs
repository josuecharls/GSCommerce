using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class MetaMensualService
    {
        private readonly HttpClient _httpClient;

        public MetaMensualService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<MetaMensualDTO>> ObtenerMetas(int? anio = null, int? mes = null, int? idAlmacen = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (anio.HasValue)
                    queryParams.Add($"anio={anio.Value}");
                if (mes.HasValue)
                    queryParams.Add($"mes={mes.Value}");
                if (idAlmacen.HasValue)
                    queryParams.Add($"idAlmacen={idAlmacen.Value}");

                var url = "api/metas-mensuales";
                if (queryParams.Any())
                    url += "?" + string.Join("&", queryParams);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return new List<MetaMensualDTO>();

                var result = await response.Content.ReadFromJsonAsync<List<MetaMensualDTO>>();
                return result ?? new List<MetaMensualDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener metas: {ex.Message}");
                return new List<MetaMensualDTO>();
            }
        }

        public async Task<MetaMensualDTO?> ObtenerMetaPorId(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/metas-mensuales/{id}");
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<MetaMensualDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener meta: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CrearMeta(CrearMetaMensualRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/metas-mensuales", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear meta: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnviarPropuesta(int id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/metas-mensuales/{id}/enviar", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar propuesta: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AprobarRechazar(AprobarMetaMensualRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/metas-mensuales/{request.IdMetaMensual}/aprobar", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al aprobar/rechazar: {ex.Message}");
                return false;
            }
        }

        public async Task<HttpResponseMessage> ExportarExcel(int id)
        {
            try
            {
                return await _httpClient.GetAsync($"api/metas-mensuales/{id}/exportar");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al exportar: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> GenerarMetasParaTodosAlmacenes(CrearMetaMensualRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/metas-mensuales/generar-todas", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar metas: {ex.Message}");
                return false;
            }
        }

        public async Task<HttpResponseMessage> ExportarExcelTodas(int anio, int mes)
        {
            try
            {
                return await _httpClient.GetAsync($"api/metas-mensuales/exportar?anio={anio}&mes={mes}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al exportar: {ex.Message}");
                throw;
            }
        }
    }
}

