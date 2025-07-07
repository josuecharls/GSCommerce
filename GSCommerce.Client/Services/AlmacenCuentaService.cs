using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class AlmacenCuentaService
    {
        private readonly HttpClient _httpClient;

        public AlmacenCuentaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AlmacenCuentaDTO>> GetCuentas(int idAlmacen)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<AlmacenCuentaDTO>>($"api/almacencuenta/{idAlmacen}");
                return result ?? new List<AlmacenCuentaDTO>();
            }
            catch
            {
                return new List<AlmacenCuentaDTO>();
            }
        }

        public async Task<bool> SaveCuentas(int idAlmacen, List<AlmacenCuentaDTO> cuentas)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/almacencuenta/{idAlmacen}", cuentas);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}