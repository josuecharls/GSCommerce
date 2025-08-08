using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class TipoPagoVentaService
    {
        private readonly HttpClient _httpClient;

        public TipoPagoVentaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TipoPagoVentaDTO>> ObtenerTodosAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TipoPagoVentaDTO>>("api/tipopagos")
                       ?? new List<TipoPagoVentaDTO>();
            }
            catch
            {
                return new List<TipoPagoVentaDTO>();
            }
        }
    }
}