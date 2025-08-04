using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class OrdenCompraService
    {
        private readonly HttpClient _httpClient;

        public OrdenCompraService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<OrdenCompraDTO?> ObtenerOrdenPorId(int id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<OrdenCompraDTO>($"api/ordenes-compra/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener orden de compra: {ex.Message}");
                return null;
            }
        }

        public async Task<List<OrdenCompraConsultaDTO>> ListarAsync(DateTime desde, DateTime hasta, int? idProveedor = null)
        {
            try
            {
                var url = $"api/ordenes-compra/list?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
                if (idProveedor.HasValue && idProveedor.Value > 0)
                    url += $"&idProveedor={idProveedor.Value}";
                var result = await _httpClient.GetFromJsonAsync<List<OrdenCompraConsultaDTO>>(url);
                return result ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al listar órdenes de compra: {ex.Message}");
                return new();
            }
        }

        public async Task<int?> CrearAsync(OrdenCompraDTO dto)
        {
            var resp = await _httpClient.PostAsJsonAsync("api/ordenes-compra", dto);
            if (resp.IsSuccessStatusCode)
            {
                var r = await resp.Content.ReadFromJsonAsync<OrdenCompraDTO>();
                return r?.IdOc;
            }
            return null;
        }

        public async Task<bool> GenerarIngresoAsync(int idOc, int idAlmacen)
        {
            var resp = await _httpClient.PostAsync($"api/ordenes-compra/{idOc}/generar-ingreso?idAlmacen={idAlmacen}", null);
            return resp.IsSuccessStatusCode;
        }
    }
}