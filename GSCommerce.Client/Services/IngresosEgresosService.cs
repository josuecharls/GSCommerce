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

    public async Task<List<IngresoEgresoDTO>> Listar(int? idAlmacen, int? idUsuario, DateTime? fechaInicio, DateTime? fechaFin, string? naturaleza)
    {
        var query = new List<string>();
        if (idAlmacen.HasValue) query.Add($"idAlmacen={idAlmacen}");
        if (idUsuario.HasValue) query.Add($"idUsuario={idUsuario}");
        if (fechaInicio.HasValue) query.Add($"fechaInicio={fechaInicio:yyyy-MM-dd}");
        if (fechaFin.HasValue) query.Add($"fechaFin={fechaFin:yyyy-MM-dd}");
        if (!string.IsNullOrWhiteSpace(naturaleza)) query.Add($"naturaleza={naturaleza}");
        var url = "api/IngresosEgresos";
        if (query.Count > 0) url += "?" + string.Join("&", query);
        var data = await _httpClient.GetFromJsonAsync<List<IngresoEgresoDTO>>(url);
        return data ?? new List<IngresoEgresoDTO>();
    }

    public async Task<bool> Registrar(IngresoEgresoRegistroDTO dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/IngresosEgresos", dto);
        return response.IsSuccessStatusCode;
    }
}