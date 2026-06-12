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
    public decimal VentaNcEmitidas { get; init; }
    public decimal VentaNcNet => VentaNcCanje - VentaNcEmitidas;
    public decimal VentaDia => VentaEfectivo + VentaTarjeta + VentaNcNet;
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

        ventasDiaQuery = from v in ventasDiaQuery
                         join c in _context.ComprobanteDeVentaCabeceras
                             on new { v.IdAlmacen, v.Serie, v.Numero, IdCajero = v.IdCajero }
                             equals new { c.IdAlmacen, c.Serie, c.Numero, IdCajero = c.IdCajero }
                         where c.Fecha >= dayStart && c.Fecha <= dayEnd
                         && !(c.Estado == "A" &&
                                 (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                                 c.FechaHoraUsuarioAnula.HasValue &&
                                 c.FechaHoraUsuarioAnula.Value >= dayStart &&
                                 c.FechaHoraUsuarioAnula.Value <= dayEnd)
                         select v;

        var ventasDia = DistinctVentasDiarias(await ventasDiaQuery.ToListAsync(cancellationToken));
        var ajustesNc = await ObtenerAjustesNcEmitidasAsync(dayStart, dayEnd, idAlmacen, null, cancellationToken);
        ventasDia.AddRange(ajustesNc);
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

        var ncEmitidasPorCajero = await ObtenerTotalesNcEmitidasPorCajeroAsync(
            dayStart, dayEnd, idAlmacen, cancellationToken);

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

            var ncEmitidas = ncEmitidasPorCajero.GetValueOrDefault((ap.IdUsuario, ap.IdAlmacen), 0m);
            var totales = CalcularTotalesVenta(ventasCajero, anuladosConNc, ncEmitidas);

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

        var ncEmitidas = await ObtenerTotalNcEmitidasAsync(
            fechaInicio, fechaFin, idAlmacen, idCajero, cancellationToken);

        return CalcularTotalesVenta(ventas, anuladosConNc, ncEmitidas);
    }

    private static CajaVentasTotales CalcularTotalesVenta(
        IEnumerable<VCierreVentaDiaria1> ventas,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)> anuladosConNc,
        decimal ncEmitidas)
    {
        AcumularTotalesVenta(ventas, out var ventaEfectivo, out var efectivoCaja, out var ventaTarjeta, out var ventaNcCanje, anuladosConNc);

        return new CajaVentasTotales
        {
            VentaEfectivo = ventaEfectivo,
            EfectivoCaja = efectivoCaja,
            VentaTarjeta = ventaTarjeta,
            VentaNcCanje = ventaNcCanje,
            VentaNcEmitidas = ncEmitidas
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
                    if (cuentaEnVentaDia)
                        ventaNc += v.Soles;
                    break;
            }
        }
    }

    private static bool CuentaEnVentaDia(
        VCierreVentaDiaria1 venta,
        IReadOnlySet<(int IdAlmacen, string Serie, int Numero)>? anuladosConNotaCredito)
    {
        if (venta.Estado != "A")
            return true;

        if (anuladosConNotaCredito == null)
            return false;

        return anuladosConNotaCredito.Contains((venta.IdAlmacen, venta.Serie, venta.Numero));
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

    private async Task<decimal> ObtenerTotalNcEmitidasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        int? idCajero,
        CancellationToken cancellationToken)
    {
        var query = _context.NotaDeCreditoCabeceras
            .AsNoTracking()
            .Where(n => n.FechaHoraRegistro >= fechaInicio && n.FechaHoraRegistro <= fechaFin);

        if (idAlmacen.HasValue)
            query = query.Where(n => n.IdAlmacen == idAlmacen.Value);

        if (idCajero.HasValue)
            query = query.Where(n => n.IdUsuario == idCajero.Value);

        return await query.SumAsync(n => n.Total, cancellationToken);
    }

    private async Task<Dictionary<(int IdUsuario, int IdAlmacen), decimal>> ObtenerTotalesNcEmitidasPorCajeroAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        CancellationToken cancellationToken)
    {
        var query = _context.NotaDeCreditoCabeceras
            .AsNoTracking()
            .Where(n => n.FechaHoraRegistro >= fechaInicio && n.FechaHoraRegistro <= fechaFin);

        if (idAlmacen.HasValue)
            query = query.Where(n => n.IdAlmacen == idAlmacen.Value);

        var filas = await query
            .GroupBy(n => new { n.IdUsuario, n.IdAlmacen })
            .Select(g => new { g.Key.IdUsuario, g.Key.IdAlmacen, Total = g.Sum(n => n.Total) })
            .ToListAsync(cancellationToken);

        return filas.ToDictionary(x => (x.IdUsuario, x.IdAlmacen), x => x.Total);
    }

    private async Task<List<VCierreVentaDiaria1>> ObtenerAjustesNcEmitidasAsync(
        DateTime fechaInicio,
        DateTime fechaFin,
        int? idAlmacen,
        int? idCajero,
        CancellationToken cancellationToken)
    {
        var query = _context.NotaDeCreditoCabeceras
            .AsNoTracking()
            .Where(n => n.Fecha >= fechaInicio &&
                        n.Fecha <= fechaFin &&
                        n.Estado != "A");

        if (idAlmacen.HasValue)
            query = query.Where(n => n.IdAlmacen == idAlmacen.Value);

        if (idCajero.HasValue)
            query = query.Where(n => n.IdUsuario == idCajero.Value);

        var notasEmitidas = await query.ToListAsync(cancellationToken);

        if (notasEmitidas.Count == 0)
            return new List<VCierreVentaDiaria1>();

        var almacenesNc = notasEmitidas.Select(n => n.IdAlmacen).Distinct().ToList();
        var clavesNc = notasEmitidas
            .Select(n => $"{n.Serie}-{n.Numero:D8}")
            .Distinct()
            .ToList();

        var paresReferencia = new List<(int IdAlmacen, string Serie, int Numero)>();
        foreach (var nc in notasEmitidas)
        {
            if (TryParseReferenciaComprobante(nc.Referencia, out var serieRef, out var numeroRef))
                paresReferencia.Add((nc.IdAlmacen, serieRef, numeroRef));
        }

        var porGeneroNc = new Dictionary<(int IdAlmacen, string ClaveNc), ComprobanteDeVentaCabecera>();
        var porReferencia = new Dictionary<(int IdAlmacen, string Referencia), List<ComprobanteDeVentaCabecera>>();

        if (clavesNc.Count > 0 || paresReferencia.Count > 0)
        {
            var series = paresReferencia.Select(p => p.Serie).Distinct().ToList();
            var numeros = paresReferencia.Select(p => p.Numero).Distinct().ToList();
            var paresSet = paresReferencia.ToHashSet();

            var comprobantes = await _context.ComprobanteDeVentaCabeceras
                .AsNoTracking()
                .Include(c => c.DetallePagoVenta)
                    .ThenInclude(p => p.IdTipoPagoVentaNavigation)
                .Where(c => almacenesNc.Contains(c.IdAlmacen) &&
                            (c.GeneroNc != null && clavesNc.Contains(c.GeneroNc) ||
                             (series.Count > 0 && numeros.Count > 0 &&
                              series.Contains(c.Serie) && numeros.Contains(c.Numero))))
                .ToListAsync(cancellationToken);

            foreach (var c in comprobantes.Where(c => !string.IsNullOrEmpty(c.GeneroNc) && clavesNc.Contains(c.GeneroNc)))
            {
                var claveNc = (c.IdAlmacen, c.GeneroNc!);
                if (!porGeneroNc.ContainsKey(claveNc))
                    porGeneroNc[claveNc] = c;
            }

            foreach (var c in comprobantes.Where(c => paresSet.Contains((c.IdAlmacen, c.Serie, c.Numero))))
            {
                var claveRef = (c.IdAlmacen, $"{c.Serie}-{c.Numero:D8}");
                if (!porReferencia.TryGetValue(claveRef, out var lista))
                {
                    lista = new List<ComprobanteDeVentaCabecera>();
                    porReferencia[claveRef] = lista;
                }
                lista.Add(c);
            }
        }

        var ajustes = new List<VCierreVentaDiaria1>();

        foreach (var nc in notasEmitidas)
        {
            var comprobanteOriginal = ResolverComprobanteOriginalNotaCredito(nc, porGeneroNc, porReferencia);
            if (comprobanteOriginal == null)
                continue;

            if (comprobanteOriginal.Estado == "A")
                continue;

            if (comprobanteOriginal.Fecha < fechaInicio || comprobanteOriginal.Fecha > fechaFin)
                continue;

            var pagosOriginales = comprobanteOriginal.DetallePagoVenta
                .Where(p => !EsPagoNotaCredito(p.IdTipoPagoVentaNavigation?.Descripcion))
                .ToList();

            if (pagosOriginales.Count == 0)
                continue;

            var reparto = RepartirMontoNcEnPagos(nc.Total, pagosOriginales);

            foreach (var (descripcionPago, monto) in reparto)
            {
                if (monto == 0) continue;

                ajustes.Add(new VCierreVentaDiaria1
                {
                    IdTipoPagoVenta = 0,
                    Descripcion = $"{descripcionPago} (N.C.)",
                    Datos = nc.Referencia,
                    Serie = nc.Serie,
                    Numero = nc.Numero,
                    Fecha = nc.Fecha,
                    Total = nc.Total,
                    Soles = -monto,
                    Dolares = 0,
                    Redondeo = 0,
                    Vuelto = 0,
                    Estado = nc.Estado,
                    IdCajero = nc.IdUsuario,
                    IdAlmacen = nc.IdAlmacen
                });
            }
        }

        return ajustes;
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

        ventasQuery = from v in ventasQuery
                      join c in _context.ComprobanteDeVentaCabeceras
                          on new { v.IdAlmacen, v.Serie, v.Numero, IdCajero = v.IdCajero }
                          equals new { c.IdAlmacen, c.Serie, c.Numero, IdCajero = c.IdCajero }
                      where c.Fecha >= fechaInicio && c.Fecha <= fechaFin
                      && !(c.Estado == "A" &&
                              (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                              c.FechaHoraUsuarioAnula.HasValue &&
                              c.FechaHoraUsuarioAnula.Value >= fechaInicio &&
                              c.FechaHoraUsuarioAnula.Value <= fechaFin)
                      select v;

        var ventas = DistinctVentasDiarias(await ventasQuery.ToListAsync(cancellationToken));
        var ajustesNc = await ObtenerAjustesNcEmitidasAsync(fechaInicio, fechaFin, idAlmacen, idCajero, cancellationToken);
        ventas.AddRange(ajustesNc);
        return ventas;
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
            TotalNc = totales.VentaNcNet,
            TotalVentas = totales.VentaDia
        };
    }

    private static ComprobanteDeVentaCabecera? ResolverComprobanteOriginalNotaCredito(
        NotaDeCreditoCabecera nc,
        IReadOnlyDictionary<(int IdAlmacen, string ClaveNc), ComprobanteDeVentaCabecera> porGeneroNc,
        IReadOnlyDictionary<(int IdAlmacen, string Referencia), List<ComprobanteDeVentaCabecera>> porReferencia)
    {
        var claveNc = $"{nc.Serie}-{nc.Numero:D8}";
        if (porGeneroNc.TryGetValue((nc.IdAlmacen, claveNc), out var porGn))
            return porGn;

        if (!TryParseReferenciaComprobante(nc.Referencia, out var serieRef, out var numeroRef))
            return null;

        var claveRef = (nc.IdAlmacen, $"{serieRef}-{numeroRef:D8}");
        if (!porReferencia.TryGetValue(claveRef, out var candidatos) || candidatos.Count != 1)
            return null;

        return candidatos[0];
    }

    private static bool TryParseReferenciaComprobante(string referencia, out string serie, out int numero)
    {
        serie = string.Empty;
        numero = 0;

        if (string.IsNullOrWhiteSpace(referencia))
            return false;

        var idx = referencia.LastIndexOf('-');
        if (idx <= 0)
            return false;

        serie = referencia[..idx];
        return int.TryParse(referencia[(idx + 1)..], out numero);
    }

    private static bool EsPagoNotaCredito(string? descripcionPago)
    {
        if (string.IsNullOrWhiteSpace(descripcionPago))
            return false;

        return descripcionPago.StartsWith("N.C.", StringComparison.OrdinalIgnoreCase);
    }

    private static List<(string Descripcion, decimal Monto)> RepartirMontoNcEnPagos(
        decimal montoNc,
        IEnumerable<DetallePagoVentum> pagos)
    {
        var pagosActivos = pagos
            .Select(p => new
            {
                Descripcion = p.IdTipoPagoVentaNavigation?.Descripcion ?? string.Empty,
                Monto = p.Soles - (p.Vuelto ?? 0)
            })
            .Where(p => p.Monto > 0 && !EsPagoNotaCredito(p.Descripcion))
            .ToList();

        if (pagosActivos.Count == 0)
            return new List<(string Descripcion, decimal Monto)>();

        var totalPagado = pagosActivos.Sum(p => p.Monto);
        if (totalPagado <= 0)
            return new List<(string Descripcion, decimal Monto)>();

        var resultado = new List<(string Descripcion, decimal Monto)>();
        decimal acumulado = 0;

        for (var i = 0; i < pagosActivos.Count; i++)
        {
            var pago = pagosActivos[i];
            var monto = i == pagosActivos.Count - 1
                ? montoNc - acumulado
                : Math.Round(montoNc * (pago.Monto / totalPagado), 2, MidpointRounding.AwayFromZero);

            acumulado += monto;
            resultado.Add((pago.Descripcion, monto));
        }

        return resultado;
    }
}
