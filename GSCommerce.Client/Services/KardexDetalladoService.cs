using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class KardexDetalladoService
    {
        private readonly HttpClient _httpClient;

        public KardexDetalladoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<KardexDetalladoDTO>> ObtenerKardexDetallado(
            string textoBusqueda,
            int? idAlmacen,
            DateTime fechaDesde,
            DateTime fechaHasta)
        {
            try
            {
                string url = $"api/kardexdetallado?articulo={textoBusqueda}&desde={fechaDesde:yyyy-MM-dd}&hasta={fechaHasta:yyyy-MM-dd}";

                if (idAlmacen.HasValue)
                    url += $"&idAlmacen={idAlmacen.Value}";

                var result = await _httpClient.GetFromJsonAsync<List<KardexDetalladoDTO>>(url);
                return result ?? new List<KardexDetalladoDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en ObtenerKardexDetallado: {ex.Message}");
                return new List<KardexDetalladoDTO>();
            }
        }
    }
}
