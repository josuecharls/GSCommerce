using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace GSCommerce.Client.Services
{
    public class ArticuloService
    {
        private readonly HttpClient _httpClient;

        public ArticuloService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener todos los articulos
        public async Task<List<ArticuloDTO>> ObtenerTodos()
        {
            var response = await _httpClient.GetFromJsonAsync<List<ArticuloDTO>>("api/articulos/todos");
            return response ?? new List<ArticuloDTO>();
        }
        public async Task<ArticuloDTO?> BuscarPorCodigoAsync(string codigo)
        {
            try
            {
                var articulo = await _httpClient.GetFromJsonAsync<ArticuloDTO>($"api/articulos/{codigo}");
                return articulo;
            }
            catch
            {
                return null;
            }
        }
        /* public async Task<List<ArticuloDTO>?> GetArticulosList()
         {
             try
             {
                 var response = await _httpClient.GetAsync("api/articulos");
                 var jsonResponse = await response.Content.ReadAsStringAsync();
                 Console.WriteLine($" JSON recibido en GetArticulos(): {jsonResponse}");

                 if (!response.IsSuccessStatusCode)
                 {
                     Console.WriteLine($"❌ Error en la API: Código {response.StatusCode}");
                     return new List<ArticuloDTO>();
                 }

                 var options = new JsonSerializerOptions
                 {
                     PropertyNameCaseInsensitive = true
                 };

                 // Deserializa como ArticuloResponse y devuelve solo la propiedad Data
                 var result = await JsonSerializer.DeserializeAsync<ArticuloResponse>(
                     await response.Content.ReadAsStreamAsync(), options);

                 return result?.Data ?? new List<ArticuloDTO>();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"❌ Excepción en GetArticulos(): {ex.Message}");
                 return new List<ArticuloDTO>();
             }
         }*/

        // Obtener la lista de artículos con paginación y búsqueda
        public async Task<ArticuloResponse> GetArticulos(int page, int pageSize, string search = "")
        {
            var response = await _httpClient.GetFromJsonAsync<ArticuloResponse>(
                $"api/articulos?page={page}&pageSize={pageSize}&search={search}");
            return response ?? new ArticuloResponse
            {
                TotalItems = 0,
                TotalPages = 0,
                Data = new List<ArticuloDTO>()
            };
        }

        public async Task<bool> UploadFoto(ArticuloDTO articulo)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/articulos/UploadFoto", articulo);
            return response.IsSuccessStatusCode;
        }

        // Obtener un artículo por ID
        public async Task<ArticuloDTO?> GetArticuloById(string id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ArticuloDTO>($"api/articulos/{id}");
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        // Obtener un nuevo ID para un artículo
        public async Task<string> GetNuevoIdArticulo()
        {
            try
            {
                return await _httpClient.GetStringAsync("api/articulos/nuevo-id");
            }
            catch (HttpRequestException)
            {
                return "000001"; // Valor por defecto si hay un error
            }
        }

        // Crear un nuevo artículo
        public async Task<bool> CreateArticulo(ArticuloDTO articulo)
        {
            var response = await _httpClient.PostAsJsonAsync("api/articulos/Upload", articulo);
            return response.IsSuccessStatusCode;
        }

        // Actualizar un artículo
        public async Task<bool> UpdateArticulo(string id, ArticuloDTO articulo)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/articulos/{id}", articulo);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando artículo: {ex.Message}");
                return false;
            }
        }

        // Eliminar un artículo
        public async Task<bool> DeleteArticulo(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/articulos/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error eliminando artículo: {ex.Message}");
                return false;
            }
        }

        public class ArticuloResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<ArticuloDTO> Data { get; set; } = new List<ArticuloDTO>();
        }
    }
}