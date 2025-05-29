using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class AuxiliaresVariosService
    {
        private readonly HttpClient _http;

        public AuxiliaresVariosService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<string>> ObtenerMotivosPorTipo(string tipo)
        {
            try
            {
                var response = await _http.GetFromJsonAsync<List<string>>($"api/auxiliares-varios/motivos/{tipo}");
                return response ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al obtener motivos: {ex.Message}");
                return new List<string>();
            }
        }

        public class MotivoDTO
        {
            public int IdAuxiliar { get; set; }
            public string Descripcion { get; set; } = "";
        }
    }
}
