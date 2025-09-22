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
    public async Task<List<LineaFiltroDTO>> ObtenerLineasArticulos()
    {
        var response = await _http.GetFromJsonAsync<List<LineaFiltroDTO>>("api/ventas/lineas-articulos");
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
    public async Task<ReportePagosTarjetaOnlineResponseDTO> ObtenerPagosTarjetaOnline(DateTime desde, DateTime hasta, int? idAlmacen = null)
    {
        var url = $"api/ventas/reporte-pagos-tarjeta-online?desde={desde:yyyy-MM-dd}&hasta={hasta:yyyy-MM-dd}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
            url += $"&idAlmacen={idAlmacen.Value}";

        var resp = await _http.GetFromJsonAsync<ReportePagosTarjetaOnlineResponseDTO>(url);
        return resp ?? new ReportePagosTarjetaOnlineResponseDTO();
    }
    public async Task<ReporteAvanceHoraDTO> ObtenerAvanceVentasHora(DateTime desde, DateTime hasta, int? idAlmacen = null)
    {
        var url = $"api/ventas/reporte-avance-por-hora?desde={Uri.EscapeDataString(desde.ToString("yyyy-MM-ddTHH:mm:ss"))}&hasta={Uri.EscapeDataString(hasta.ToString("yyyy-MM-ddTHH:mm:ss"))}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
        {
            url += $"&idAlmacen={idAlmacen.Value}";
        }

        var resp = await _http.GetFromJsonAsync<ReporteAvanceHoraDTO>(url);
        return resp ?? new ReporteAvanceHoraDTO
        {
            HoraInicio = desde,
            HoraFin = hasta,
            TotalVentas = 0m,
            TotalVentasDia = 0m,
            Tickets = 0,
            TicketsDia = 0,
            DetalleHoras = new List<ReporteAvanceHoraDetalleDTO>()
        };
    }

    public async Task<List<ReporteArticuloRangoDTO>> ObtenerReporteArticulosRango(
    List<string> ids, DateTime desde, DateTime hasta)
    {
        var body = new ReporteArticulosRangoRequest { Ids = ids, Desde = desde, Hasta = hasta };
        var resp = await _http.PostAsJsonAsync("api/ventas/reporte-articulos-rango", body);
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<ReporteArticuloRangoDTO>>() ?? new();
    }

    public async Task<List<ReporteGastosInversionDTO>> ObtenerReporteGastos(int anio, int mes, int? idAlmacen = null)
    {
        var url = $"api/reportes/gastos?anio={anio}&mes={mes}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
        {
            url += $"&idAlmacen={idAlmacen.Value}";
        }

        var resp = await _http.GetFromJsonAsync<List<ReporteGastosInversionDTO>>(url);
        return resp ?? new();
    }

    public async Task<bool> GuardarReporteGastos(GuardarReporteGastosRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/reportes/gastos/detalles", request);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> GuardarReporteGastosAlquiler(GuardarReporteGastosAlquilerRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/reportes/gastos/alquiler", request);
        return resp.IsSuccessStatusCode;
    }

    public async Task<HttpResponseMessage> ExportarReporteGastosExcel(int anio, int mes, int? idAlmacen = null)
    {
        var url = $"api/reportes/gastos/exportar?anio={anio}&mes={mes}";
        if (idAlmacen.HasValue && idAlmacen.Value > 0)
        {
            url += $"&idAlmacen={idAlmacen.Value}";
        }

        return await _http.GetAsync(url);
    }
}
