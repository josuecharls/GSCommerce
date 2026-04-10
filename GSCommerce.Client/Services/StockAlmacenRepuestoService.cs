using System.Net.Http.Json;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services;

public class StockAlmacenRepuestoService
{
    private readonly HttpClient _httpClient;

    public StockAlmacenRepuestoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<StockAlmacenRepuestoLineaDTO>> GetStockAsync(int idAlmacenRepuesto)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<StockAlmacenRepuestoLineaDTO>>(
                $"stockalmacenrepuesto?idAlmacenRepuesto={idAlmacenRepuesto}");
            return result ?? new List<StockAlmacenRepuestoLineaDTO>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetStockAsync repuesto: {ex.Message}");
            return new List<StockAlmacenRepuestoLineaDTO>();
        }
    }

    public async Task<(bool Ok, string? Error)> AjustarAsync(AjusteStockRepuestoRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("stockalmacenrepuesto/ajuste", request);
            if (response.IsSuccessStatusCode)
                return (true, null);

            var msg = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(msg) ? response.ReasonPhrase : msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
