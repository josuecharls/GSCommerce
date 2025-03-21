using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class ArticuloService
    {
        private readonly HttpClient _httpClient;

        public ArticuloService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Obtener la lista de artículos con paginación y búsqueda
        public async Task<ArticuloResponse> GetArticulos(int page, int pageSize, string search = "")
        {
            var response = await _httpClient.GetFromJsonAsync<ArticuloResponse>(
                $"api/articulos?page={page}&pageSize={pageSize}&search={search}");
            return response ?? new ArticuloResponse
            {
                TotalItems = 0,
                TotalPages = 0,
                Data = []
            };
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
            var response = await _httpClient.PostAsJsonAsync("api/articulos", articulo);
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

        // Subir imagen del artículo
        public async Task<bool> UploadImage(string id, string base64Image)
        {
            var request = new ArticuloDTO { IdArticulo = id, Foto = base64Image };
            var response = await _httpClient.PutAsJsonAsync($"api/articulos/UploadFoto/{id}", request);
            return response.IsSuccessStatusCode;
        }

        public class ArticuloResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<ArticuloDTO> Data { get; set; } = new List<ArticuloDTO>();
        }
    }
}
