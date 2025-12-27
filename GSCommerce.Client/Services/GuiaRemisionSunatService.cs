using System.Net.Http.Json;
using GSCommerce.Client.Models.SUNAT;

namespace GSCommerce.Client.Services;

public class GuiaRemisionSunatService
{
    private readonly HttpClient _http;

    public GuiaRemisionSunatService(HttpClient http)
    {
        _http = http;
    }

    public async Task<GuiaRemisionListadoResponse?> ListarGuiasAsync(
        int? idAlmacen = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? estadoSunat = null,
        int page = 1,
        int pageSize = 10)
    {
        var url = $"guias-remision-sunat?page={page}&pageSize={pageSize}";

        if (idAlmacen.HasValue)
            url += $"&idAlmacen={idAlmacen}";
        if (fechaDesde.HasValue)
            url += $"&fechaDesde={fechaDesde:yyyy-MM-dd}";
        if (fechaHasta.HasValue)
            url += $"&fechaHasta={fechaHasta:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(estadoSunat))
            url += $"&estadoSunat={estadoSunat}";

        return await _http.GetFromJsonAsync<GuiaRemisionListadoResponse>(url);
    }

    public async Task<GuiaRemisionSunatDTO?> ObtenerGuiaAsync(int id)
    {
        return await _http.GetFromJsonAsync<GuiaRemisionSunatDTO>($"guias-remision-sunat/{id}");
    }

    public async Task<(bool Ok, string Mensaje, int IdGuiaRemision)> CrearGuiaAsync(GuiaRemisionSunatDTO dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("guias-remision-sunat", dto);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CrearGuiaResponse>();
                return (true, result?.Mensaje ?? "Guía creada", result?.IdGuiaRemision ?? 0);
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Mensaje ?? "Error al crear guía", 0);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, 0);
        }
    }

    public async Task<(bool Ok, string Mensaje)> ActualizarGuiaAsync(GuiaRemisionSunatDTO dto)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"guias-remision-sunat/{dto.IdGuiaRemision}", dto);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MensajeResponse>();
                return (true, result?.Mensaje ?? "Guía actualizada");
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Mensaje ?? "Error al actualizar guía");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string Mensaje)> AnularGuiaAsync(int id)
    {
        try
        {
            var response = await _http.PutAsync($"guias-remision-sunat/{id}/anular", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MensajeResponse>();
                return (true, result?.Mensaje ?? "Guía anulada");
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Mensaje ?? "Error al anular guía");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Anular una GRE ya aceptada en SUNAT (Comunicación de Baja)
    /// </summary>
    public async Task<(bool Ok, string Mensaje, string? Ticket)> AnularGuiaSunatAsync(int id, string motivoBaja)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"guias-remision-sunat/{id}/anular-sunat", new { motivoBaja });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EnviarSunatResponse>();
                return (true, result?.Mensaje ?? "Baja comunicada", result?.Ticket);
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Mensaje ?? "Error al comunicar baja a SUNAT", null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Ok, string Mensaje, string? Ticket)> EnviarSunatAsync(int id)
    {
        try
        {
            var response = await _http.PostAsync($"guias-remision-sunat/{id}/enviar-sunat", null);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<EnviarSunatResponse>();
                return (true, result?.Mensaje ?? "Guía enviada", result?.Ticket);
            }
            
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Mensaje ?? "Error al enviar a SUNAT", null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool Ok, string Codigo, string Mensaje)> ConsultarEstadoSunatAsync(int id)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<ConsultarEstadoResponse>($"guias-remision-sunat/{id}/estado-sunat");
            return (result?.Exito ?? false, result?.Codigo ?? "", result?.Mensaje ?? "");
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    public async Task<List<CatalogoItemDTO>> ObtenerMotivosTraslado()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CatalogoItemDTO>>("guias-remision-sunat/catalogos/motivos-traslado") ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<CatalogoItemDTO>> ObtenerModalidadesTransporte()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CatalogoItemDTO>>("guias-remision-sunat/catalogos/modalidades-transporte") ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<CatalogoItemDTO>> ObtenerTiposDocumento()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<CatalogoItemDTO>>("guias-remision-sunat/catalogos/tipos-documento") ?? new();
        }
        catch
        {
            return new();
        }
    }

    private class CrearGuiaResponse
    {
        public string? Mensaje { get; set; }
        public int IdGuiaRemision { get; set; }
    }

    private class MensajeResponse
    {
        public string? Mensaje { get; set; }
    }

    private class ErrorResponse
    {
        public string? Mensaje { get; set; }
    }

    private class EnviarSunatResponse
    {
        public string? Mensaje { get; set; }
        public string? Ticket { get; set; }
    }

    private class ConsultarEstadoResponse
    {
        public bool Exito { get; set; }
        public string? Codigo { get; set; }
        public string? Mensaje { get; set; }
    }
}


