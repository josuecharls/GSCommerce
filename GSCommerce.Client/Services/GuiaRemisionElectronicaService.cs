using GSCommerce.Client.Models.SUNAT;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class GuiaRemisionElectronicaService
    {
        private readonly HttpClient _httpClient;

        public GuiaRemisionElectronicaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<GuiaRemisionDTO?> ObtenerGuia(int idGuiaRemision)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<GuiaRemisionDTO>($"api/guia-remision-electronica/{idGuiaRemision}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener guía: {ex.Message}");
                return null;
            }
        }

        public async Task<GuiaRemisionResponse?> ListarGuias(int page = 1, int pageSize = 10, string? search = null, int? idAlmacen = null)
        {
            try
            {
                var url = $"api/guia-remision-electronica/list?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(search))
                    url += $"&search={search}";
                if (idAlmacen.HasValue)
                    url += $"&idAlmacen={idAlmacen}";

                return await _httpClient.GetFromJsonAsync<GuiaRemisionResponse>(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al listar guías: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool exito, string mensaje)> GenerarXmlGuia(GuiaRemisionDTO guia)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/guia-remision-electronica/generar", guia);
                var resultado = await response.Content.ReadFromJsonAsync<GenerarGuiaResponse>();

                if (response.IsSuccessStatusCode && resultado != null)
                {
                    return (true, resultado.mensaje ?? "XML generado correctamente");
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Error al generar XML: {ex.Message}");
            }
        }

        public async Task<(bool exito, string mensaje)> EnviarGuia(GuiaRemisionDTO guia)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/guia-remision-electronica/enviar", guia);
                var resultado = await response.Content.ReadFromJsonAsync<EnviarGuiaResponse>();

                if (response.IsSuccessStatusCode && resultado != null)
                {
                    return (true, resultado.mensaje ?? "Guía enviada correctamente");
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, error);
            }
            catch (Exception ex)
            {
                return (false, $"Error al enviar guía: {ex.Message}");
            }
        }

        public async Task<string> ConsultarTicket(int idGuiaRemision, string ticket)
        {
            try
            {
                var request = new { Ticket = ticket };
                var response = await _httpClient.PostAsJsonAsync($"api/guia-remision-electronica/consultar-ticket/{idGuiaRemision}", request);
                var resultado = await response.Content.ReadFromJsonAsync<ConsultarTicketResponse>();

                if (response.IsSuccessStatusCode && resultado != null)
                {
                    return resultado.mensaje ?? "Consulta realizada";
                }

                return "Error al consultar ticket";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<(bool exito, string mensaje, GuiaRemisionDTO? guia)> CrearGuiaDesdeMovimiento(int idMovimiento, GuiaRemisionDTO guia)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/guia-remision-electronica/crear-desde-movimiento/{idMovimiento}", guia);
                
                if (response.IsSuccessStatusCode)
                {
                    var resultado = await response.Content.ReadFromJsonAsync<CrearGuiaResponse>();
                    return (true, resultado?.mensaje ?? "Guía creada correctamente", resultado?.guia);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, error, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error al crear guía: {ex.Message}", null);
            }
        }

        // Clases de respuesta
        public class GuiaRemisionResponse
        {
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
            public List<GuiaRemisionListItem> Data { get; set; } = new();
        }

        public class GuiaRemisionListItem
        {
            public int IdGuiaRemision { get; set; }
            public int IdMovimiento { get; set; }
            public string? Serie { get; set; }
            public int? Numero { get; set; }
            public DateTime? Fecha { get; set; }
            public string? RazonSocialNombre { get; set; }
            public string? Rucdnice { get; set; }
            public string? Estado { get; set; }
            public string? Almacen { get; set; }
            public string? EstadoSunat { get; set; }
            public bool EnviadoSunat { get; set; }
            public DateTime? FechaEnvio { get; set; }
        }

        public class GenerarGuiaResponse
        {
            public string? mensaje { get; set; }
            public string? hash { get; set; }
            public string? ruta { get; set; }
        }

        public class EnviarGuiaResponse
        {
            public string? mensaje { get; set; }
        }

        public class ConsultarTicketResponse
        {
            public string? mensaje { get; set; }
        }

        public class CrearGuiaResponse
        {
            public string? mensaje { get; set; }
            public GuiaRemisionDTO? guia { get; set; }
        }
    }
}
