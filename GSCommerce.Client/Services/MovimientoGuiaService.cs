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
        public async Task<MovimientoGuiaResponse> GetGuiasList(int page, int pageSize, string search = "", string tipo = "")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/movimientos-guias/list?page={page}&pageSize={pageSize}&search={search}&tipo={tipo}");
                var jsonResponse = await response.Content.ReadAsStringAsync(); // 🔹 Imprime la respuesta
                Console.WriteLine($"🔹 JSON recibido en GetGuiasList(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new MovimientoGuiaResponse
                    {
                        TotalItems = 0,
                        TotalPages = 0,
                        Data = new List<MovimientoGuiaDTO>()
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<MovimientoGuiaResponse>();
                return result ?? new MovimientoGuiaResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<MovimientoGuiaDTO>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetGuiasList(): {ex.Message}");
                return new MovimientoGuiaResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<MovimientoGuiaDTO>()
                };
            }
        }

        public async Task<bool> ConfirmarTransferencia(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/movimientos-guias/{id}/confirmar", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al confirmar transferencia: {ex.Message}");
                return false;
            }
        }

        // 🔴 Anular guía
        public async Task<bool> AnularGuia(int id)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/movimientos-guias/{id}/anular", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al anular guía: {ex.Message}");
                return false;
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

        // 🆕 Crear
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

        // ✏️ Actualizar
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

        // 🗑️ Eliminar
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

        public class MovimientoGuiaResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<MovimientoGuiaDTO> Data { get; set; } = new();
        }
    }
}