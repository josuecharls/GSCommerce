using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class NotaCreditoService
    {
        private readonly HttpClient _http;

        public NotaCreditoService(HttpClient http)
        {
            _http = http;
        }

        public async Task<NotaCreditoResponseDTO?> EmitirNotaCreditoAsync(string tipo, NotaCreditoRegistroDTO dto)
        {
            var response = await _http.PostAsJsonAsync($"/api/notascredito/emitir/{tipo.ToLower()}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NotaCreditoResponseDTO>();
            }

            return null;
        }
    }
}
