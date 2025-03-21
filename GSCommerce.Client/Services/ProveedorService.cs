using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class ProveedorService
    {
        private readonly HttpClient _httpClient;

        public ProveedorService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener todos los proveedores
        public async Task<List<ProveedorDTO>?> GetProveedores()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/proveedor");
                var jsonResponse = await response.Content.ReadAsStringAsync(); // 🔹 Imprime el JSON
                Console.WriteLine($"🔹 JSON recibido en GetProveedores(): {jsonResponse}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new List<ProveedorDTO>();
                }
                var result = await response.Content.ReadFromJsonAsync<List<ProveedorDTO>>();
                return result ?? new List<ProveedorDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetProveedores(): {ex.Message}");
                return new List<ProveedorDTO>();
            }
        }

        // Obtener todos los proveedores con paginación y búsqueda
        public async Task<ProveedorResponse> GetProveedorList(int page, int pageSize, string search)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/proveedor/list?page={page}&pageSize={pageSize}&search={search}");
                var jsonResponse = await response.Content.ReadAsStringAsync(); // 🔹 Imprime la respuesta
                Console.WriteLine($"🔹 JSON recibido en GetProveedorList(): {jsonResponse}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new ProveedorResponse
                    {
                        TotalItems = 0,
                        TotalPages = 0,
                        Data = new List<ProveedorDTO>()
                    };
                }
                var result = await response.Content.ReadFromJsonAsync<ProveedorResponse>();
                return result ?? new ProveedorResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<ProveedorDTO>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetProveedorList(): {ex.Message}");
                return new ProveedorResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<ProveedorDTO>()
                };
            }
        }

        // Obtener un proveedor por ID
        public async Task<ProveedorDTO?> GetProveedorById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ProveedorDTO>($"api/proveedor/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Crear un nuevo proveedor
        public async Task<bool> CreateProveedor(ProveedorDTO proveedor)
        {
            var response = await _httpClient.PostAsJsonAsync("api/proveedor", proveedor);
            return response.IsSuccessStatusCode;
        }

        // Actualizar un proveedor existente
        public async Task<bool> UpdateProveedor(int id, ProveedorDTO proveedor)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/proveedor/{id}", proveedor);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando proveedor: {ex.Message}");
                return false;
            }
        }

        // Eliminar un proveedor
        public async Task<bool> DeleteProveedor(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/proveedor/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando proveedor: {ex.Message}");
                return false;
            }
        }

        // Clase para manejar la respuesta de la API con paginación
        public class ProveedorResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<ProveedorDTO> Data { get; set; } = new List<ProveedorDTO>();
        }
    }
}
