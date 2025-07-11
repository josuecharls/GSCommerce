using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class PrecioAlmacenService
    {
        private readonly HttpClient _httpClient;
        public PrecioAlmacenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<PrecioAlmacenDTO>> ObtenerLista(int idAlmacen)
        {
            try
            {
                var res = await _httpClient.GetFromJsonAsync<List<PrecioAlmacenDTO>>($"api/PreciosAlmacen/{idAlmacen}");
                return res ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<PrecioAlmacenDTO?> ObtenerPrecio(int idAlmacen, string idArticulo)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PrecioAlmacenDTO>($"api/PreciosAlmacen/{idAlmacen}/{idArticulo}");
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> GuardarDescuento(PrecioAlmacenDTO dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/PreciosAlmacen", new
            {
                IdAlmacen = dto.IdAlmacen,
                IdArticulo = dto.IdArticulo,
                Descuento = dto.Descuento ?? 0
            });
            return response.IsSuccessStatusCode;
        }
    }
}