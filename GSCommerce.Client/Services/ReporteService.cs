using GSCommerce.Client.Models.DTOs.Reportes;
using System.Net.Http.Json;

public class ReporteArticulosRangoRequest
{
    public List<string> Ids { get; set; } = new();
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
}

public class ReporteService
{
    private readonly HttpClient _http;

    public ReporteService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ReporteArticuloDTO?> ObtenerReportePorArticulo(string idArticulo, int mes, int anio)
    {
        var url = $"api/ventas/reporte-articulo?idArticulo={idArticulo}&mes={mes}&anio={anio}";
        var response = await _http.GetFromJsonAsync<ReporteArticuloDTO>(url);
        return response;
    }

    public async Task<List<RankingVendedoraDTO>> ObtenerRankingVendedoras(DateTime desde, DateTime hasta, int? idAlmacen, bool porAlmacen)
    {
        var url = $"api/ventas/reporte-ranking-vendedoras?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}&porAlmacen={porAlmacen}";
        if (porAlmacen && idAlmacen.HasValue)
        {
            url += $"&idAlmacen={idAlmacen.Value}";
        }
        var response = await _http.GetFromJsonAsync<List<RankingVendedoraDTO>>(url);
        return response ?? new();
    }

    public async Task<List<TopArticuloDTO>> ObtenerTop10Articulos(
        DateTime desde,
        DateTime hasta,
        int? idAlmacen = null,
        string? linea = null)
    {
        var url = $"api/ventas/reporte-top10-articulos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            url += $"&idAlmacen={idAlmacen.Value}";
        if (!string.IsNullOrWhiteSpace(linea))
            url += $"&linea={Uri.EscapeDataString(linea)}";
        var response = await _http.GetFromJsonAsync<List<TopArticuloDTO>>(url);
        return response ?? new();
    }

    public async Task<List<TopArticuloDTO>> ObtenerTop10ArticulosMenosVendidos(
        DateTime desde,
        DateTime hasta,
        int? idAlmacen = null,
        string? linea = null)
    {
        var url = $"api/ventas/reporte-top10-articulos-menos-vendidos?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            url += $"&idAlmacen={idAlmacen.Value}";
        if (!string.IsNullOrWhiteSpace(linea))
            url += $"&linea={Uri.EscapeDataString(linea)}";

        var response = await _http.GetFromJsonAsync<List<TopArticuloDTO>>(url);
        return response ?? new();
    }

    public async Task<List<ReporteTotalTiendasDTO>> ObtenerTotalTiendas(DateTime desde, DateTime hasta, int? idAlmacen = null)
    {
        var url = $"api/ventas/reporte-total-tiendas?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            url += $"&idAlmacen={idAlmacen.Value}";

        var resp = await _http.GetFromJsonAsync<List<ReporteTotalTiendasDTO>>(url);
        return resp ?? new();
    }
    public async Task<List<ReporteUtilidadTiendasDTO>> ObtenerUtilidadTiendas(DateTime desde, DateTime hasta, int? idAlmacen = null)
    {
        var url = $"api/ventas/reporte-utilidad-tiendas?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            url += $"&idAlmacen={idAlmacen.Value}";

        var resp = await _http.GetFromJsonAsync<List<ReporteUtilidadTiendasDTO>>(url);
        return resp ?? new();
    }

    public async Task<List<ReporteArticuloRangoDTO>> ObtenerReporteArticulosRango(
    List<string> ids, DateTime desde, DateTime hasta)
    {
        var body = new ReporteArticulosRangoRequest { Ids = ids, Desde = desde, Hasta = hasta };
        var resp = await _http.PostAsJsonAsync("api/ventas/reporte-articulos-rango", body);
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<ReporteArticuloRangoDTO>>() ?? new();
    }
}
