using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Services.Caja;

public sealed class CajaVentasTotales
{
    public decimal VentaEfectivo { get; init; }
    public decimal EfectivoCaja { get; init; }
    public decimal VentaTarjeta { get; init; }
    public decimal VentaNcCanje { get; init; }
    public decimal VentaDia => VentaEfectivo + VentaTarjeta;
}

public sealed class VentasDiariasCajaResponse
{
    public List<VCierreVentaDiaria1> Ventas { get; set; } = new();
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalNc { get; set; }
    public decimal TotalVentas { get; set; }
}

public sealed class CajaListadoDiaItem
{
    public int IdAperturaCierre { get; set; }
    public int IdAlmacen { get; set; }
    public string NombreAlmacen { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public int IdUsuario { get; set; }
    public string Cajero { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; }
    public decimal VentaDia { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Egresos { get; set; }
    public decimal SaldoFinal { get; set; }
    public List<VCierreVentaDiaria1> Ventas { get; set; } = new();
}

public interface ICajaVentasDiariasService
{
    Task<List<CajaListadoDiaItem>> ObtenerListadoDiaAsync(DateOnly fecha, int? idAlmacen = null, CancellationToken cancellationToken = default);

    Task<List<VCierreVentaDiaria1>> ObtenerVentasDiariasFiltradasAsync(
        int idAlmacen,
        int idCajero,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);

    Task<VentasDiariasCajaResponse> ObtenerVentasDiariasConTotalesAsync(
        int idAlmacen,
        int idCajero,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default);

    Task<CajaVentasTotales> CalcularTotalesVentaAsync(
        IEnumerable<VCierreVentaDiaria1> ventas,
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        int? idCajero,
        CancellationToken cancellationToken = default);

    Task<List<VCierreVentaDiaria1>> ObtenerVentasDiariasPorRangoAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default);

    Task<HashSet<(int IdAlmacen, string Serie, int Numero)>> ObtenerAnuladosConNotaCreditoAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default);
}

public class CajaVentasDiariasService : ICajaVentasDiariasService
{
    private readonly SyscharlesContext _context;

    public CajaVentasDiariasService(SyscharlesContext context)
    {
        _context = context;
    }

    public async Task<List<CajaListadoDiaItem>> ObtenerListadoDiaAsync(
        DateOnly fecha,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default)
    {
        var dayStart = fecha.ToDateTime(TimeOnly.MinValue);
        var dayEnd = fecha.ToDateTime(new TimeOnly(23, 59, 59));

        var aperturas = await _context.AperturaCierreCajas
            .AsNoTracking()
            .Include(a => a.IdAlmacenNavigation)
            .Include(a => a.IdUsuarioNavigation).ThenInclude(u => u.IdPersonalNavigation)
            .Where(a => a.Fecha == fecha && (!idAlmacen.HasValue || a.IdAlmacen == idAlmacen.Value))
            .OrderBy(a => a.IdAlmacen).ThenBy(a => a.IdUsuario)
            .ToListAsync(cancellationToken);

        if (aperturas.Count == 0)
            return new List<CajaListadoDiaItem>();

        var claves = aperturas.Select(a => (a.IdUsuario, a.IdAlmacen)).ToHashSet();

        var ventasDiaQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.Fecha >= dayStart && v.Fecha <= dayEnd)
            .Where(v => !idAlmacen.HasValue || v.IdAlmacen == idAlmacen.Value);

        ventasDiaQuery = AplicarJoinComprobante(ventasDiaQuery, dayStart, dayEnd);

        var ventasDia = DistinctVentasDiarias(await ventasDiaQuery.ToListAsync(cancellationToken));
        ventasDia = ventasDia.Where(v => claves.Contains((v.IdCajero, v.IdAlmacen))).ToList();

        var ventasPorClave = ventasDia
            .GroupBy(v => (v.IdCajero, v.IdAlmacen))
            .ToDictionary(g => g.Key, g => g.ToList());

        var movsDia = await _context.IngresosEgresosCabeceras
            .AsNoTracking()
            .Where(m => m.Fecha >= dayStart && m.Fecha <= dayEnd && m.Estado == "E")
            .Select(m => new { m.IdUsuario, m.IdAlmacen, m.Naturaleza, Grupo = (m.Tipo ?? "").Trim(), m.Monto })
            .ToListAsync(cancellationToken);
        movsDia = movsDia.Where(m => claves.Contains((m.IdUsuario, m.IdAlmacen))).ToList();

        decimal Ingresos(int u, int a) => movsDia.Where(m => m.IdUsuario == u && m.IdAlmacen == a && m.Naturaleza == "I").Sum(m => m.Monto);
        decimal Transf(int u, int a) => movsDia.Where(m => m.IdUsuario == u && m.IdAlmacen == a && m.Naturaleza == "E" && m.Grupo.StartsWith("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Monto);
        decimal Prov(int u, int a) => movsDia.Where(m => m.IdUsuario == u && m.IdAlmacen == a && m.Naturaleza == "E" && m.Grupo.Equals("PAGO PROVEEDORES", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Monto);
        decimal Gastos(int u, int a)
        {
            var exactos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "PAGO LUZ", "PAGO AGUA", "PAGO TELEFONO", "PAGO CELULAR", "PAGO ALQUILER",
                "PAGO PLANILLAS", "PAGO ADELANTOS", "SUNAT", "FLETE", "AFP", "AFP-REPORTE",
                "SUNAT-REPORTE", "ALQUILER-REPORTE"
            };
            return movsDia.Where(m => m.IdUsuario == u && m.IdAlmacen == a && m.Naturaleza == "E" &&
                                      (m.Grupo.StartsWith("GASTOS DIVERSOS", StringComparison.OrdinalIgnoreCase) || exactos.Contains(m.Grupo)))
                .Sum(m => m.Monto);
        }

        var anuladosConNc = await ObtenerClavesAnuladosConNotaCreditoAsync(
            dayStart, dayEnd, idAlmacen, null, cancellationToken);

        var resultado = new List<CajaListadoDiaItem>();

        foreach (var ap in aperturas)
        {
            var saldoDiaAnterior = await _context.AperturaCierreCajas
                .AsNoTracking()
                .Where(a => a.IdAlmacen == ap.IdAlmacen
                         && a.IdUsuario == ap.IdUsuario
                         && a.Fecha < ap.Fecha
                         && a.Estado == "C")
                .OrderByDescending(a => a.Fecha)
                .Select(a => (decimal?)a.SaldoFinal)
                .FirstOrDefaultAsync(cancellationToken) ?? ap.SaldoInicial;

            var ventasCajero = ventasPorClave.TryGetValue((ap.IdUsuario, ap.IdAlmacen), out var vList)
                ? vList
                : new List<VCierreVentaDiaria1>();

            var totales = CalcularTotalesVenta(ventasCajero, anuladosConNc);

            var ingresos = Ingresos(ap.IdUsuario, ap.IdAlmacen);
            var egresos = Gastos(ap.IdUsuario, ap.IdAlmacen) + Transf(ap.IdUsuario, ap.IdAlmacen) + Prov(ap.IdUsuario, ap.IdAlmacen);

            resultado.Add(new CajaListadoDiaItem
            {
                IdAperturaCierre = ap.IdAperturaCierre,
                IdAlmacen = ap.IdAlmacen,
                NombreAlmacen = ap.IdAlmacenNavigation?.Nombre ?? string.Empty,
                RazonSocial = ap.IdAlmacenNavigation?.RazonSocial,
                IdUsuario = ap.IdUsuario,
                Cajero = $"{ap.IdUsuarioNavigation?.IdPersonalNavigation?.Nombres} {ap.IdUsuarioNavigation?.IdPersonalNavigation?.Apellidos}".Trim(),
                Estado = ap.Estado,
                SaldoInicial = saldoDiaAnterior,
                VentaDia = totales.VentaDia,
                Ingresos = ingresos,
                Egresos = egresos,
                SaldoFinal = saldoDiaAnterior + totales.EfectivoCaja + ingresos - egresos,
                Ventas = ventasCajero
            });
        }

        return resultado;
    }

    internal static List<VCierreVentaDiaria1> DistinctVentasDiarias(IEnumerable<VCierreVentaDiaria1> rows)
    {
        return rows
            .GroupBy(v => (
                v.IdAlmacen,
                v.IdCajero,
                v.IdTipoPagoVenta,
                v.Descripcion,
                v.Datos,
                v.Serie,
                v.Numero,
                v.Fecha,
                v.Total,
                v.Soles,
                v.Dolares,
                v.Redondeo,
                v.Vuelto,
                v.Estado))
            .Select(g => g.First())
            .ToList();
    }

    public async Task<CajaVentasTotales> CalcularTotalesVentaAsync(
        IEnumerable<VCierreVentaDiaria1> ventas,
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        int? idCajero,
        CancellationToken cancellationToken = default)
    {
        var anuladosConNc = await ObtenerClavesAnuladosConNotaCreditoAsync(
            fechaInicio, fechaFin, idAlmacen, idCajero, cancellationToken);

        return CalcularTotalesVenta(ventas, anuladosConNc);
    }

    internal static CajaVentasTotales CalcularTotalesVenta(
        IEnumerable<VCierreVentaDiaria1> ventas,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)> anuladosConNc)
    {
        AcumularTotalesVenta(ventas, out var ventaEfectivo, out var efectivoCaja, out var ventaTarjeta, out var ventaNcCanje, anuladosConNc);

        return new CajaVentasTotales
        {
            VentaEfectivo = ventaEfectivo,
            EfectivoCaja = efectivoCaja,
            VentaTarjeta = ventaTarjeta,
            VentaNcCanje = ventaNcCanje
        };
    }

    internal static void AcumularTotalesVenta(
        IEnumerable<VCierreVentaDiaria1> ventas,
        out decimal ventaEfectivo,
        out decimal efectivoCaja,
        out decimal ventaTarjeta,
        out decimal ventaNc,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)>? anuladosConNotaCredito = null)
    {
        ventaEfectivo = 0;
        efectivoCaja = 0;
        ventaTarjeta = 0;
        ventaNc = 0;

        foreach (var v in ventas)
        {
            var tipo = v.Descripcion.Split(' ')[0];
            var cuentaEnVentaDia = CuentaEnVentaDia(v, anuladosConNotaCredito);

            switch (tipo)
            {
                case "Efectivo":
                    var netoEfectivo = v.Soles - (v.Vuelto ?? 0);
                    efectivoCaja += netoEfectivo;
                    if (cuentaEnVentaDia)
                        ventaEfectivo += netoEfectivo;
                    break;
                case "Tarjeta":
                case "Online":
                    if (cuentaEnVentaDia)
                        ventaTarjeta += v.Soles;
                    break;
                case "N.C.":
                    if (cuentaEnVentaDia && EsPagoCanjeNc(v.Descripcion))
                        ventaNc += v.Soles;
                    break;
            }
        }
    }

    private static bool EsPagoCanjeNc(string descripcion) =>
        descripcion.StartsWith("N.C.", StringComparison.OrdinalIgnoreCase);

    internal static bool CuentaEnVentaDia(
        VCierreVentaDiaria1 venta,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)>? anuladosConNotaCredito)
    {
        if (venta.Estado != "A")
            return true;

        if (anuladosConNotaCredito == null)
            return false;

        return anuladosConNotaCredito.Contains((venta.IdAlmacen, venta.Serie, venta.Numero));
    }

    private IQueryable<VCierreVentaDiaria1> AplicarJoinComprobante(
        IQueryable<VCierreVentaDiaria1> ventasQuery,
        DateTime fechaInicio,
        DateTime fechaFin)
    {
        return from v in ventasQuery
               join c in _context.ComprobanteDeVentaCabeceras
                   on new { v.IdAlmacen, v.Serie, v.Numero, IdCajero = v.IdCajero, Total = v.Total }
                   equals new { c.IdAlmacen, c.Serie, c.Numero, IdCajero = c.IdCajero, Total = c.Total }
               where c.Fecha >= fechaInicio && c.Fecha <= fechaFin
               && !(c.Estado == "A" &&
                       (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                       c.FechaHoraUsuarioAnula.HasValue &&
                       c.FechaHoraUsuarioAnula.Value >= fechaInicio &&
                       c.FechaHoraUsuarioAnula.Value <= fechaFin)
               select v;
    }

    private async Task<HashSet<(int IdAlmacen, string Serie, int Numero)>> ObtenerClavesAnuladosConNotaCreditoAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        int? idCajero,
        CancellationToken cancellationToken)
    {
        var query = _context.ComprobanteDeVentaCabeceras
            .AsNoTracking()
            .Where(c => c.Estado == "A" &&
                        c.GeneroNc != null &&
                        c.Fecha >= fechaInicio &&
                        c.Fecha <= fechaFin);

        if (idAlmacen.HasValue)
            query = query.Where(c => c.IdAlmacen == idAlmacen.Value);

        if (idCajero.HasValue)
            query = query.Where(c => c.IdCajero == idCajero.Value);

        var claves = await query
            .Select(c => new { c.IdAlmacen, c.Serie, c.Numero })
            .ToListAsync(cancellationToken);

        return claves
            .Select(c => (c.IdAlmacen, c.Serie, c.Numero))
            .ToHashSet();
    }

    public async Task<List<VCierreVentaDiaria1>> ObtenerVentasDiariasFiltradasAsync(
        int idAlmacen,
        int idCajero,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.IdAlmacen == idAlmacen &&
                        v.IdCajero == idCajero &&
                        v.Fecha >= fechaInicio &&
                        v.Fecha <= fechaFin);

        ventasQuery = AplicarJoinComprobante(ventasQuery, fechaInicio, fechaFin);

        return DistinctVentasDiarias(await ventasQuery.ToListAsync(cancellationToken));
    }

    public async Task<List<VCierreVentaDiaria1>> ObtenerVentasDiariasPorRangoAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default)
    {
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.Fecha >= fechaInicio && v.Fecha <= fechaFin);

        if (idAlmacen.HasValue)
            ventasQuery = ventasQuery.Where(v => v.IdAlmacen == idAlmacen.Value);

        ventasQuery = AplicarJoinComprobante(ventasQuery, fechaInicio, fechaFin);

        return DistinctVentasDiarias(await ventasQuery.ToListAsync(cancellationToken));
    }

    public Task<HashSet<(int IdAlmacen, string Serie, int Numero)>> ObtenerAnuladosConNotaCreditoAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen = null,
        CancellationToken cancellationToken = default) =>
        ObtenerClavesAnuladosConNotaCreditoAsync(fechaInicio, fechaFin, idAlmacen, null, cancellationToken);

    internal static int ContarComprobantesEnVentaDia(
        IEnumerable<VCierreVentaDiaria1> ventas,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)> anuladosConNc)
    {
        return ventas
            .Where(v =>
            {
                var tipo = v.Descripcion.Split(' ')[0];
                return tipo is "Efectivo" or "Tarjeta" or "Online"
                       && CuentaEnVentaDia(v, anuladosConNc);
            })
            .Select(v => (v.IdAlmacen, v.Serie, v.Numero, v.Total))
            .Distinct()
            .Count();
    }

    public async Task<VentasDiariasCajaResponse> ObtenerVentasDiariasConTotalesAsync(
        int idAlmacen,
        int idCajero,
        DateTime fechaInicio,
        DateTime fechaFin,
        CancellationToken cancellationToken = default)
    {
        var ventas = await ObtenerVentasDiariasFiltradasAsync(
            idAlmacen, idCajero, fechaInicio, fechaFin, cancellationToken);

        var totales = await CalcularTotalesVentaAsync(
            ventas, fechaInicio, fechaFin, idAlmacen, idCajero, cancellationToken);

        return new VentasDiariasCajaResponse
        {
            Ventas = ventas,
            TotalEfectivo = totales.VentaEfectivo,
            TotalTarjeta = totales.VentaTarjeta,
            TotalNc = totales.VentaNcCanje,
            TotalVentas = totales.VentaDia
        };
    }
}
