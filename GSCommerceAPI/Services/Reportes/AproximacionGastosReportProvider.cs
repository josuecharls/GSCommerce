using GSCommerceAPI.Data;
using GSCommerceAPI.Services.Caja;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Services.Reportes;

public sealed class AproximacionGastosTiendaFila
{
    public int IdAlmacen { get; set; }
    public string Tienda { get; set; } = string.Empty;
    public decimal Venta { get; set; }
    public decimal Utilidad { get; set; }
    public decimal Reinvertir => Math.Round(Utilidad * 0.10m, 2);
}

public sealed class AproximacionGastosReporte
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public List<AproximacionGastosTiendaFila> Filas { get; set; } = new();
    public decimal TotalVenta => Filas.Sum(f => f.Venta);
    public decimal TotalUtilidad => Filas.Sum(f => f.Utilidad);
    public decimal TotalReinvertir => Filas.Sum(f => f.Reinvertir);
}

public interface IAproximacionGastosReportProvider
{
    Task<AproximacionGastosReporte> ObtenerReporteAsync(DateOnly hasta, CancellationToken cancellationToken = default);
}

public class AproximacionGastosReportProvider : IAproximacionGastosReportProvider
{
    private readonly SyscharlesContext _context;

    public AproximacionGastosReportProvider(SyscharlesContext context)
    {
        _context = context;
    }

    public async Task<AproximacionGastosReporte> ObtenerReporteAsync(
        DateOnly hasta,
        CancellationToken cancellationToken = default)
    {
        var desdeDate = new DateTime(hasta.Year, hasta.Month, 1);
        var hastaDate = hasta.ToDateTime(TimeOnly.MinValue);
        var hastaExcl = hastaDate.AddDays(1);

        var almacenesActivos = (await _context.Almacens
                .AsNoTracking()
                .Where(a => a.Estado)
                .Select(a => a.IdAlmacen)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        // Ventas: misma lógica que reporte-utilidad-tiendas / reporte-total-tiendas
        var ventasQuery = _context.VCierreVentaDiaria1s
            .AsNoTracking()
            .Where(v => v.Fecha >= desdeDate && v.Fecha < hastaExcl);

        ventasQuery = from v in ventasQuery
                      join c in _context.ComprobanteDeVentaCabeceras
                          on new { v.IdAlmacen, v.Serie, v.Numero, IdCajero = v.IdCajero, Total = v.Total }
                          equals new { c.IdAlmacen, c.Serie, c.Numero, IdCajero = c.IdCajero, Total = c.Total }
                      where c.Fecha >= v.Fecha.Date && c.Fecha < v.Fecha.Date.AddDays(1)
                            && !(c.Estado == "A" &&
                                 (string.IsNullOrEmpty(c.GeneroNc) || c.ExcluirDeEstadisticas) &&
                                 c.FechaHoraUsuarioAnula.HasValue &&
                                 c.FechaHoraUsuarioAnula.Value.Date == v.Fecha.Date)
                      select v;

        var ventas = CajaVentasDiariasService.DistinctVentasDiarias(await ventasQuery.ToListAsync(cancellationToken))
            .Where(v => almacenesActivos.Contains(v.IdAlmacen))
            .ToList();

        var montosVenta = ventas
            .GroupBy(v => v.IdAlmacen)
            .Select(g =>
            {
                decimal vEfectivo = 0m, vTarjeta = 0m;
                foreach (var v in g)
                {
                    var tipo = (v.Descripcion ?? string.Empty).Split(' ')[0];
                    switch (tipo)
                    {
                        case "Efectivo":
                            vEfectivo += v.Soles - (v.Vuelto ?? 0m);
                            break;
                        case "Tarjeta":
                        case "Online":
                            vTarjeta += v.Soles;
                            break;
                    }
                }

                return new { IdAlmacen = g.Key, Venta = vEfectivo + vTarjeta };
            })
            .Where(x => x.Venta > 0m)
            .ToDictionary(x => x.IdAlmacen, x => x.Venta);

        if (montosVenta.Count == 0)
        {
            return new AproximacionGastosReporte
            {
                Desde = DateOnly.FromDateTime(desdeDate),
                Hasta = hasta,
                Filas = new List<AproximacionGastosTiendaFila>()
            };
        }

        var idsConVenta = montosVenta.Keys.ToList();

        // Costos: misma lógica que reporte-utilidad-tiendas
        var lineas =
            from d in _context.VDetallesVentas
            where d.Fecha >= desdeDate && d.Fecha < hastaExcl
                  && idsConVenta.Contains(d.IdAlmacen)
            select new
            {
                d.IdAlmacen,
                d.Almacen,
                Total = d.Total ?? 0m,
                CostoLineaBruto = (d.Costo ?? 0m) != 0m
                    ? (d.Costo ?? 0m)
                    : (d.PrecioCompra * (decimal)(d.Cantidad ?? 0))
            };

        var montosCosto = await
            (from x in lineas
             let costoAjustado = x.Total >= 0m ? x.CostoLineaBruto : -x.CostoLineaBruto
             group new { x.Total, costoAjustado } by new { x.IdAlmacen, x.Almacen } into g
             select new
             {
                 g.Key.IdAlmacen,
                 Tienda = g.Key.Almacen,
                 Costo = g.Sum(s => s.costoAjustado)
             })
            .ToListAsync(cancellationToken);

        var costosPorAlmacen = montosCosto.ToDictionary(x => x.IdAlmacen, x => x.Costo);
        var nombresDesdeCosto = montosCosto
            .Where(x => !string.IsNullOrWhiteSpace(x.Tienda))
            .ToDictionary(x => x.IdAlmacen, x => x.Tienda!);

        var nombresAlmacen = await _context.Almacens
            .AsNoTracking()
            .Where(a => idsConVenta.Contains(a.IdAlmacen))
            .Select(a => new { a.IdAlmacen, a.Nombre })
            .ToDictionaryAsync(a => a.IdAlmacen, a => a.Nombre, cancellationToken);

        var filas = montosVenta
            .Select(kvp =>
            {
                var venta = Math.Round(kvp.Value, 2);
                var costo = costosPorAlmacen.TryGetValue(kvp.Key, out var c) ? c : 0m;
                var utilidad = Math.Round(venta - costo, 2);

                string tienda;
                if (nombresDesdeCosto.TryGetValue(kvp.Key, out var desdeCosto) && !string.IsNullOrWhiteSpace(desdeCosto))
                    tienda = desdeCosto;
                else if (nombresAlmacen.TryGetValue(kvp.Key, out var nombre) && !string.IsNullOrWhiteSpace(nombre))
                    tienda = nombre;
                else
                    tienda = $"Almacén {kvp.Key}";

                return new AproximacionGastosTiendaFila
                {
                    IdAlmacen = kvp.Key,
                    Tienda = tienda,
                    Venta = venta,
                    Utilidad = utilidad
                };
            })
            .OrderBy(f => f.Tienda, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AproximacionGastosReporte
        {
            Desde = DateOnly.FromDateTime(desdeDate),
            Hasta = hasta,
            Filas = filas
        };
    }
}
