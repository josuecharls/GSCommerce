using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class ClienteService
    {
        private readonly HttpClient _httpClient;

        public ClienteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<ClienteDTO>?> GetClientes()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/clientes");
                var jsonResponse = await response.Content.ReadAsStringAsync();
              //  Console.WriteLine($" JSON recibido en GetClientes(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new List<ClienteDTO>();
                }

                var result = await response.Content.ReadFromJsonAsync<List<ClienteDTO>>();
                return result ?? new List<ClienteDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetClientes(): {ex.Message}");
                return new List<ClienteDTO>();
            }
        }

        public async Task<ClienteResponse> GetClienteList(int page, int pageSize, string search = "")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/clientes/list?page={page}&pageSize={pageSize}&search={search}");
                var jsonResponse = await response.Content.ReadAsStringAsync();
                //Console.WriteLine($" JSON recibido en GetClienteList(): {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                    return new ClienteResponse
                    {
                        TotalItems = 0,
                        TotalPages = 0,
                        Data = new List<ClienteDTO>()
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<ClienteResponse>();
                return result ?? new ClienteResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<ClienteDTO>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Excepción en GetClienteList(): {ex.Message}");
                return new ClienteResponse
                {
                    TotalItems = 0,
                    TotalPages = 0,
                    Data = new List<ClienteDTO>()
                };
            }
        }

        public async Task<ClienteDTO?> GetClienteById(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ClienteDTO>($"api/clientes/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<bool> CreateCliente(ClienteDTO cliente)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/clientes", cliente);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear cliente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCliente(int id, ClienteDTO cliente)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/clientes/{id}", cliente);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando cliente: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCliente(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/clientes/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando cliente: {ex.Message}");
                return false;
            }
        }
        public async Task<ClienteDTO?> GetClienteByDocumento(string documento)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/clientes/documento/{documento}");
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<ClienteDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error GetClienteByDocumento: {ex.Message}");
                return null;
            }
        }

        public class ClienteResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<ClienteDTO> Data { get; set; } = new List<ClienteDTO>();
        }
    }
}
