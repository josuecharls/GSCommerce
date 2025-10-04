using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class MetaDiariaService
    {
        private readonly HttpClient _httpClient;

        public MetaDiariaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<decimal> ObtenerMetaDiaria(int idAlmacen, DateOnly fecha)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<decimal>($"api/ventas/meta-diaria?idAlmacen={idAlmacen}&fecha={fecha:yyyy-MM-dd}");
                return response;
            }
            catch
            {
                return 0m; // Meta predeterminada si hay error
            }
        }

        public async Task<bool> GuardarMetaDiaria(int idAlmacen, DateOnly fecha, decimal meta)
        {
            try
            {
                var request = new MetaDiariaRequest
                {
                    IdAlmacen = idAlmacen,
                    Fecha = fecha,
                    Meta = meta
                };

                var response = await _httpClient.PostAsJsonAsync("api/ventas/meta-diaria", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> GuardarMetaDiariaMultiple(int[] idAlmacenes, DateOnly fecha, decimal meta)
        {
            try
            {
                var request = new MetaDiariaMultipleRequest
                {
                    IdAlmacenes = idAlmacenes,
                    Fecha = fecha,
                    Meta = meta
                };

                var response = await _httpClient.PostAsJsonAsync("api/ventas/meta-diaria-multiple", request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<MetaDiariaResponse>> ObtenerMetasDiarias(DateOnly fecha, int? idAlmacen = null)
        {
            try
            {
                var url = $"api/ventas/metas-diarias?fecha={fecha:yyyy-MM-dd}";
                if (idAlmacen.HasValue)
                {
                    url += $"&idAlmacen={idAlmacen.Value}";
                }

                var response = await _httpClient.GetFromJsonAsync<List<MetaDiariaResponse>>(url);
                return response ?? new List<MetaDiariaResponse>();
            }
            catch
            {
                return new List<MetaDiariaResponse>();
            }
        }
    }

    // DTOs para las metas
    public class MetaDiariaRequest
    {
        public int IdAlmacen { get; set; }
        public DateOnly Fecha { get; set; }
        public decimal Meta { get; set; }
    }

    public class MetaDiariaMultipleRequest
    {
        public int[] IdAlmacenes { get; set; } = Array.Empty<int>();
        public DateOnly Fecha { get; set; }
        public decimal Meta { get; set; }
    }

    public class MetaDiariaResponse
    {
        public int IdAlmacen { get; set; }
        public DateOnly Fecha { get; set; }
        public decimal Meta { get; set; }
        public string Descripcion { get; set; } = string.Empty;
    }
}
