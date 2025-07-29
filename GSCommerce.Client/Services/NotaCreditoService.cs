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


        public async Task<List<NotaCreditoConsultaDTO>> ObtenerNotasAsync(DateTime desde, DateTime hasta, string? dniruc = null, int idAlmacen = 0)
        {
            try
            {
                var url = $"/api/api/notascredito/list?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";

                if (!string.IsNullOrWhiteSpace(dniruc))
                    url += $"&dniruc={dniruc}";

                if (idAlmacen > 0)
                    url += $"&idAlmacen={idAlmacen}";

                var result = await _http.GetFromJsonAsync<List<NotaCreditoConsultaDTO>>(url);
                return result ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar notas de crédito: {ex.Message}");
                return new();
            }
        }
    }
}
