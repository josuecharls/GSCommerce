using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GSCommerce.Client.Services
{
    public class ReniecService
    {
        private readonly HttpClient _httpClient;
        private const string token = "c83184858ed421c01fb50eab0a55baac1ca2c54516781f6a0da1e03b568d5d54"; // reemplaza con tu token real

        public ReniecService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> ObtenerNombrePorDNI(string dni)
        {
            try
            {
                var url = $"https://apiperu.dev/api/dni/{dni}?api_token={token}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error en la API: {response.StatusCode}");
                    return "ERROR AL CONSULTAR RENIEC";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<DniRespuesta>(jsonResponse);

                if (resultado?.success != true)
                {
                    return "DNI NO ENCONTRADO";
                }

                // Extrae los nombres y apellidos (ajusta según la estructura real)
                var nombres = resultado.data?.nombres ?? "";
                var apPaterno = resultado.data?.apellido_paterno ?? resultado.data?.apellido_paterno ?? "";
                var apMaterno = resultado.data?.apellido_materno ?? resultado.data?.apellido_materno ?? "";

                return $"{nombres} {apPaterno} {apMaterno}".Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar RENIEC: {ex.Message}");
                return "ERROR EN CONEXIÓN";
            }
        }

        public async Task<string?> ObtenerNombrePorRUC(string ruc)
        {
            try
            {
                var url = $"https://apiperu.dev/api/ruc/{ruc}?api_token={token}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error en la API: {response.StatusCode}");
                    return "ERROR AL CONSULTAR SUNAT";
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta RUC: {jsonResponse}"); // Para depuración

                var resultado = JsonSerializer.Deserialize<RucRespuesta>(jsonResponse);

                if (resultado?.success == true)
                {
                    // Devuelve razón social + estado entre paréntesis
                    return $"{resultado.data?.RazonSocial} ({resultado.data?.estado})";
                }

                return "RUC NO ENCONTRADO";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar SUNAT: {ex.Message}");
                return "ERROR EN CONEXIÓN";
            }
        }

        public class DniRespuesta
        {
            public bool success { get; set; }
            public DniData? data { get; set; }
        }

        public class DniData
        {
            public string? nombres { get; set; }
            [JsonPropertyName("apellido_paterno")] // Asegúrate que coincida con la respuesta JSON
            public string? apellido_paterno { get; set; }
            [JsonPropertyName("apellido_materno")]
            public string? apellido_materno { get; set; }
        }

        public class RucRespuesta
        {
            public bool success { get; set; }
            public RucData? data { get; set; }
        }

        public class RucData
        {
            [JsonPropertyName("nombre_o_razon_social")]
            public string? RazonSocial { get; set; }

            public string? estado { get; set; }
            public string? condicion { get; set; }
            public string? direccion { get; set; }

            [JsonPropertyName("direccion_completa")]
            public string? DireccionCompleta { get; set; }
        }
    }
}
