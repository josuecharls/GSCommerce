using GSCommerce.Client.Models;
using System.Net.Http.Json;

public class TipoCambioService
{
    private readonly HttpClient _http;

    public TipoCambioService(HttpClient http)
    {
        _http = http;
    }

    public async Task<TipoCambioDTO?> ObtenerTipoCambioDeHoyAsync()
    {
        return await _http.GetFromJsonAsync<TipoCambioDTO>("api/tipocambio/hoy");
    }

    public async Task<List<TipoCambioDTO>> ObtenerPorMes(int mes, int anio)
    {
        return await _http.GetFromJsonAsync<List<TipoCambioDTO>>($"api/tipocambio/mes/{mes}/{anio}");
    }

    public async Task<bool> Insertar(TipoCambioDTO nuevo)
    {
        var response = await _http.PostAsJsonAsync("api/tipocambio", nuevo);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Modificar(TipoCambioDTO modificado)
    {
        var response = await _http.PutAsJsonAsync("api/tipocambio", modificado);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> Eliminar(DateTime fecha)
    {
        var fechaStr = fecha.ToString("yyyy-MM-dd");
        var response = await _http.DeleteAsync($"api/tipocambio/{fechaStr}");
        return response.IsSuccessStatusCode;
    }
}