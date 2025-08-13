using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class ComplementoService
    {
        private readonly HttpClient _http;
        public ComplementoService(HttpClient http) => _http = http;

        public async Task<List<ComplementoDTO>> GetByTipo(string tipo)
            => await _http.GetFromJsonAsync<List<ComplementoDTO>>($"api/complementos/{Uri.EscapeDataString(tipo)}")
               ?? new List<ComplementoDTO>();

        public async Task<ComplementoDTO?> Crear(string tipo, string descripcion, string? alias)
        {
            var payload = new ComplementoDTO
            {
                Complemento = tipo,
                Descripcion = descripcion,
                Alias = alias
            };
            var resp = await _http.PostAsJsonAsync("api/complementos", payload);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<ComplementoDTO>();
        }
    }
}
