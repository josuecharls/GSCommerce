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
                return await _httpClient.GetFromJsonAsync<OrdenCompraDTO>($"ordenes-compra/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener orden de compra: {ex.Message}");
                return null;
            }
        }

        public async Task<List<OrdenCompraConsultaDTO>> ListarAsync(DateTime desde, DateTime hasta, int? idProveedor = null, string? searchItem = null)
        {
            try
            {
                var url = $"ordenes-compra/list?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
                if (idProveedor.HasValue && idProveedor.Value > 0)
                    url += $"&idProveedor={idProveedor.Value}";
                if (!string.IsNullOrWhiteSpace(searchItem))
                    url += $"&searchItem={Uri.EscapeDataString(searchItem.Trim())}";
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
            var resp = await _httpClient.PostAsJsonAsync("ordenes-compra", dto);
            if (resp.IsSuccessStatusCode)
            {
                var r = await resp.Content.ReadFromJsonAsync<OrdenCompraDTO>();
                return r?.IdOc;
            }
            return null;
        }

        public async Task<bool> ActualizarAsync(OrdenCompraDTO dto)
        {
            var resp = await _httpClient.PutAsJsonAsync($"ordenes-compra/{dto.IdOc}", dto);
            return resp.IsSuccessStatusCode;
        }

        public async Task<GenerarIngresoResult> GenerarIngresoAsync(int idOc, int idAlmacen = 1009)
        {
            var resp = await _httpClient.PostAsync($"ordenes-compra/{idOc}/generar-ingreso?idAlmacen={idAlmacen}", null);
            if (resp.IsSuccessStatusCode)
            {
                var data = await resp.Content.ReadFromJsonAsync<GenerarIngresoResponse>();
                return new GenerarIngresoResult
                {
                    Success = true,
                    IdMovimiento = data?.IdMovimiento,
                    Estado = data?.Estado
                };
            }

            var mensaje = await resp.Content.ReadAsStringAsync();
            return new GenerarIngresoResult
            {
                Success = false,
                ErrorMessage = string.IsNullOrWhiteSpace(mensaje) ? resp.ReasonPhrase : mensaje
            };
        }


        public async Task<OperacionResult> AnularAsync(int idOc)
        {
            try
            {
                var resp = await _httpClient.PostAsync($"ordenes-compra/{idOc}/anular", null);
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadFromJsonAsync<OperacionResponse>();
                    return new OperacionResult
                    {
                        Success = true,
                        Message = data?.Message ?? "Orden anulada correctamente",
                        Estado = data?.Estado
                    };
                }

                var mensaje = await resp.Content.ReadAsStringAsync();
                return new OperacionResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(mensaje) ? resp.ReasonPhrase : mensaje
                };
            }
            catch (Exception ex)
            {
                return new OperacionResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private class OperacionResponse
        {
            public string? Estado { get; set; }

            public string? Message { get; set; }
        }
    }
}