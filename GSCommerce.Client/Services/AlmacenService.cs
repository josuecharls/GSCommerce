using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services
{
    public class AlmacenService
    {
        private readonly HttpClient _httpClient;

        public AlmacenService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<AlmacenDTO>?> GetAlmacenes()
        {
            return await _httpClient.GetFromJsonAsync<List<AlmacenDTO>>("api/almacen");
        }
    }

    public class AlmacenDto
    {
        public int IdAlmacen { get; set; }
        public required string Nombre { get; set; }
    }
}