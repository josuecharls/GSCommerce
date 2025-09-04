using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class MovimientoGuiaService
    {
        private readonly HttpClient _httpClient;

        public MovimientoGuiaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener lista de guías con paginación y búsqueda
        public async Task<MovimientoGuiaResponse> GetGuiasList(
            int page, int pageSize, string search = "", string tipo = "", string motivo = "", int? idAlmacen = null)
        {
            try
            {
                var url = $"api/movimientos-guias/list?page={page}&pageSize={pageSize}&search={search}&tipo={tipo}&motivo={motivo}"; if (idAlmacen.HasValue && idAlmacen.Value > 0)
                    url += $"&idAlmacen={idAlmacen.Value}";

                var response = await _httpClient.GetAsync(url);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($" JSON recibido en GetGuiasList(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new MovimientoGuiaResponse();
                }

                return await response.Content.ReadFromJsonAsync<MovimientoGuiaResponse>()
                       ?? new MovimientoGuiaResponse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetGuiasList(): {ex.Message}");
                return new MovimientoGuiaResponse();
            }
        }

        public async Task<ApiResponse> ConfirmarTransferencia(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/movimientos-guias/{id}/confirmar", null);
                var mensaje = await ObtenerMensaje(response);
                return new ApiResponse
                {
                    Ok = response.IsSuccessStatusCode,
                    Mensaje = mensaje
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al confirmar transferencia: {ex.Message}");
                return new ApiResponse { Ok = false, Mensaje = $"Error al confirmar transferencia: {ex.Message}" };
            }
        }

        private static async Task<string> ObtenerMensaje(HttpResponseMessage response)
        {
            try
            {
                var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (data != null)
                {
                    if (data.TryGetValue("mensaje", out var msg)) return msg;
                    if (data.TryGetValue("message", out var msg2)) return msg2;
                }
            }
            catch { }

            return response.IsSuccessStatusCode ? "Operación realizada" : "Ocurrió un error";
        }

        public class ApiResponse
        {
            public bool Ok { get; set; }
            public string Mensaje { get; set; } = string.Empty;
        }

        //  Anular guía
        public async Task<ApiResponse> AnularGuia(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/movimientos-guias/{id}/anular", null);
                var mensaje = await ObtenerMensaje(response);
                return new ApiResponse
                {
                    Ok = response.IsSuccessStatusCode,
                    Mensaje = mensaje
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al anular guía: {ex.Message}");
                return new ApiResponse { Ok = false, Mensaje = $"Error al anular guía: {ex.Message}" };
            }
        }

        public async Task<MovimientoGuiaDTO?> GetMovimientoById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<MovimientoGuiaDTO>($"api/movimientos-guias/{id}");
            }
            catch
            {
                return null;
            }
        }


        // Crear
        public async Task<bool> CreateMovimiento(MovimientoGuiaDTO movimiento)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/movimientos-guias", movimiento);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear movimiento: {ex.Message}");
                return false;
            }
        }

        // Actualizar
        public async Task<bool> UpdateMovimiento(int id, MovimientoGuiaDTO movimiento)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/movimientos-guias/{id}", movimiento);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al actualizar movimiento: {ex.Message}");
                return false;
            }
        }

        // Eliminar
        public async Task<bool> DeleteMovimiento(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/movimientos-guias/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al eliminar movimiento: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CreateMovimientoConSP(MovimientoGuiaDTO guia)
        {
            var response = await _httpClient.PostAsJsonAsync("api/movimientos-guias/registrar-con-sp", guia);
            return response.IsSuccessStatusCode;
        }

        public class MovimientoGuiaResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<MovimientoGuiaDTO> Data { get; set; } = new();
        }
    }
}