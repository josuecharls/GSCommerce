using GSCommerce.Client.Models;
using System.Net.Http.Json;

namespace GSCommerce.Client.Services;

public class SerieGuiaRemisionService
{
    private readonly HttpClient _httpClient;

    public SerieGuiaRemisionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Obtener todas las series de guías de remisión
    /// </summary>
    public async Task<List<SerieGuiaRemisionDTO>> ObtenerTodasAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<SerieGuiaRemisionDTO>>(
                "serie-guia-remision");
            return response ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al obtener series: {ex.Message}");
            return new();
        }
    }

    /// <summary>
    /// Obtener la serie asignada a un almacén específico
    /// </summary>
    public async Task<SerieGuiaRemisionDTO?> ObtenerPorAlmacenAsync(int idAlmacen)
    {
        try
        {
            var response = await _httpClient.GetAsync($"serie-guia-remision/almacen/{idAlmacen}");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<SerieGuiaRemisionDTO>();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al obtener serie del almacén: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Crear una nueva serie
    /// </summary>
    public async Task<(bool exito, string mensaje)> CrearSerieAsync(SerieGuiaRemisionDTO dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("serie-guia-remision", new
            {
                dto.IdAlmacen,
                dto.Serie,
                dto.Correlativo
            });

            var result = await response.Content.ReadFromJsonAsync<RespuestaApi>();

            if (response.IsSuccessStatusCode)
                return (true, result?.Mensaje ?? "Serie creada correctamente");
            
            return (false, result?.Mensaje ?? "Error al crear la serie");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualizar correlativo o estado de una serie
    /// </summary>
    public async Task<(bool exito, string mensaje)> ActualizarSerieAsync(SerieGuiaRemisionDTO dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"serie-guia-remision/{dto.IdSerieGuiaRemision}", new
            {
                dto.Correlativo,
                dto.Estado
            });

            var result = await response.Content.ReadFromJsonAsync<RespuestaApi>();

            if (response.IsSuccessStatusCode)
                return (true, result?.Mensaje ?? "Serie actualizada correctamente");
            
            return (false, result?.Mensaje ?? "Error al actualizar la serie");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Eliminar (desactivar) una serie
    /// </summary>
    public async Task<(bool exito, string mensaje)> EliminarSerieAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"serie-guia-remision/{id}");
            var result = await response.Content.ReadFromJsonAsync<RespuestaApi>();

            if (response.IsSuccessStatusCode)
                return (true, result?.Mensaje ?? "Serie eliminada correctamente");
            
            return (false, result?.Mensaje ?? "Error al eliminar la serie");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    private class RespuestaApi
    {
        public string? Mensaje { get; set; }
    }
}
