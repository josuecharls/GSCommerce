using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services;

public class IngresosEgresosService
{
    private readonly HttpClient _httpClient;

    public IngresosEgresosService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<IngresoEgresoDTO>> Listar(int? idAlmacen, int? idUsuario, DateTime? fechaInicio, DateTime? fechaFin, string? naturaleza, string? tipo)
    {
        var query = new List<string>();
        if (idAlmacen.HasValue) query.Add($"idAlmacen={idAlmacen}");
        if (idUsuario.HasValue) query.Add($"idUsuario={idUsuario}");
        if (fechaInicio.HasValue) query.Add($"fechaInicio={fechaInicio:yyyy-MM-dd}");
        if (fechaFin.HasValue) query.Add($"fechaFin={fechaFin:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(naturaleza))
        {
            var nat = naturaleza.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "E";
            query.Add($"naturaleza={nat}");
        }
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            query.Add($"tipo={Uri.EscapeDataString(tipo)}");
        }
        var url = "api/IngresosEgresos";
        if (query.Count > 0) url += "?" + string.Join("&", query);
        var data = await _httpClient.GetFromJsonAsync<List<IngresoEgresoDTO>>(url);
        return data ?? new List<IngresoEgresoDTO>();
    }

    public async Task<bool> Registrar(IngresoEgresoRegistroDTO dto)
    {
        dto.Naturaleza = dto.Naturaleza.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "E";
        var response = await _httpClient.PostAsJsonAsync("api/IngresosEgresos", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Anular(int id)
    {
        var response = await _httpClient.PutAsync($"api/IngresosEgresos/{id}/anular", null);
        return response.IsSuccessStatusCode;
    }
}