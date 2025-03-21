using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class AlmacenService
    {
        private readonly HttpClient _httpClient;

        public AlmacenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AlmacenDTO>?> GetAlmacenes()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/almacen");
                var jsonResponse = await response.Content.ReadAsStringAsync(); // 🔹 Imprime el JSON
                Console.WriteLine($"🔹 JSON recibido en GetAlmacenes(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new List<AlmacenDTO>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<AlmacenDTO>>();
                return result ?? new List<AlmacenDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetAlmacenes(): {ex.Message}");
                return new List<AlmacenDTO>();
            }
        }


        // Obtener lista de almacenes con paginación y búsqueda
        public async Task<AlmacenResponse> GetAlmacenList(int page, int pageSize, string search = "")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/almacen/list?page={page}&pageSize={pageSize}&search={search}");
                var jsonResponse = await response.Content.ReadAsStringAsync(); // 🔹 Imprime la respuesta
                Console.WriteLine($"🔹 JSON recibido en GetAlmacenList(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new AlmacenResponse
                    {
                        TotalItems = 0,
                        TotalPages = 0,
                        Data = new List<AlmacenDTO>()
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<AlmacenResponse>();
                return result ?? new AlmacenResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<AlmacenDTO>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetAlmacenList(): {ex.Message}");
                return new AlmacenResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<AlmacenDTO>()
                };
            }
        }

        // Obtener un almacén por ID
        public async Task<AlmacenDTO?> GetAlmacenById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AlmacenDTO>($"api/almacen/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Crear un nuevo almacén
        public async Task<bool> CreateAlmacen(AlmacenDTO almacen)
        {
            var response = await _httpClient.PostAsJsonAsync("api/almacen", almacen);
            return response.IsSuccessStatusCode;
        }

        // Actualizar un almacén existente
        public async Task<bool> UpdateAlmacen(int id, AlmacenDTO almacen)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/almacen/{id}", almacen);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando almacen: {ex.Message}");
                return false;
            }
        }

        // Eliminar un almacén por ID
        public async Task<bool> DeleteAlmacen(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/almacen/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando almacen: {ex.Message}");
                return false;
            }
        }

        // Clase para manejar la respuesta con paginación
        public class AlmacenResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<AlmacenDTO> Data { get; set; } = new List<AlmacenDTO>();
        }
    }
}