using System.Net.Http;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services
{
    public class NotificacionService
    {
        private readonly HttpClient _httpClient;

        public NotificacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<NotificacionDTO>> GetNotificaciones(bool soloNoLeidas = true)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<NotificacionDTO>>(
                    $"api/notificaciones?soloNoLeidas={soloNoLeidas}");
                return response ?? new List<NotificacionDTO>();
            }
            catch
            {
                return new List<NotificacionDTO>();
            }
        }

        public async Task<bool> MarcarComoLeida(int idNotificacion)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/notificaciones/{idNotificacion}/marcar-leida", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    public class NotificacionDTO
    {
        public int IdNotificacion { get; set; }
        public int IdMovimiento { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int NumeroGuia { get; set; }
        public string? EstadoGuia { get; set; }
    }
}


