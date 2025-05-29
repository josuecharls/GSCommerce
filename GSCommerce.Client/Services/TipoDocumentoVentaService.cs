using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class TipoDocumentoVentaService
    {
        private readonly HttpClient _httpClient;

        public TipoDocumentoVentaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<TipoDocumentoVentaDTO>> GetTiposDocumento()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<TipoDocumentoVentaDTO>>("api/tipodocumentos")
                       ?? new List<TipoDocumentoVentaDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error cargando tipos de documento: {ex.Message}");
                return new List<TipoDocumentoVentaDTO>();
            }
        }
    }
}