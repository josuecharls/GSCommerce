using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class ArticuloVarianteService
    {
        private readonly HttpClient _http;

        public ArticuloVarianteService(HttpClient http)
        {
            _http = http;
        }

        private const string baseUrl = "api/ArticuloVariante";

        public async Task<List<ArticuloVarianteDTO>> ObtenerVariantesPorArticulo(string idArticulo)
        {
            var response = await _http.GetFromJsonAsync<List<ArticuloVarianteDTO>>($"{baseUrl}/articulo/{idArticulo}");
            return response ?? new List<ArticuloVarianteDTO>();
        }

        public async Task<(bool Success, string? ErrorMessage)> RegistrarVariante(ArticuloVarianteDTO variante)
        {
            var response = await _http.PostAsJsonAsync(baseUrl, variante);
            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return (false, errorMessage);
            }
        }

        public async Task<bool> EliminarVariante(int idVariante)
        {
            var response = await _http.DeleteAsync($"{baseUrl}/{idVariante}");
            return response.IsSuccessStatusCode;
        }
    }
}
