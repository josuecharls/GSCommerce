using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using GSCommerce.Client.Models;

namespace GSCommerce.Client.Services;

public class DescuentoService
{
    private readonly HttpClient _httpClient;
    public DescuentoService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<DescuentoDTO>> ObtenerDescuentos(int idAlmacen)
    {
        var response = await _httpClient.GetFromJsonAsync<List<DescuentoDTO>>($"api/descuentos/{idAlmacen}");
        return response ?? new List<DescuentoDTO>();
    }

    public async Task<bool> AgregarDescuento(int idAlmacen, string idArticulo, double porcentaje)
    {
        var dto = new { IdAlmacen = idAlmacen, IdArticulo = idArticulo, DescuentoPorc = porcentaje };
        var resp = await _httpClient.PostAsJsonAsync("api/descuentos", dto);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ModificarDescuento(int idAlmacen, string idArticulo, double porcentaje)
    {
        var dto = new { IdAlmacen = idAlmacen, IdArticulo = idArticulo, DescuentoPorc = porcentaje };
        var resp = await _httpClient.PutAsJsonAsync("api/descuentos", dto);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> EliminarDescuento(int idAlmacen, string idArticulo)
    {
        var resp = await _httpClient.DeleteAsync($"api/descuentos/{idAlmacen}/{idArticulo}");
        return resp.IsSuccessStatusCode;
    }
}