using System.Globalization;
using GSCommerceAPI.Services.Caja;

namespace GSCommerceAPI.Services.Reportes;

public sealed class CuadroVentasEfectivoFila
{
    public string Tienda { get; set; } = string.Empty;
    public decimal VentaDia { get; set; }
    public decimal EfectivoSaldoCaja { get; set; }
}

public sealed class CuadroVentasEfectivoReporte
{
    public DateOnly Fecha { get; set; }
    public List<CuadroVentasEfectivoFila> Filas { get; set; } = new();
    public decimal TotalVentaDia { get; set; }
    public decimal TotalEfectivo { get; set; }
}

public sealed class BilleteraVirtualReporte
{
    public DateOnly Fecha { get; set; }
    public List<string> RazonesSociales { get; set; } = new();
    public List<BilleteraVirtualFila> Filas { get; set; } = new();
    public Dictionary<string, decimal> TotalesPorRazon { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal TotalGeneral { get; set; }
}

public sealed class BilleteraVirtualFila
{
    public string Tipo { get; set; } = string.Empty;
    public Dictionary<string, decimal> MontosPorRazon { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public decimal Total { get; set; }
}

public interface ICuadroCajaReportProvider
{
    Task<CuadroVentasEfectivoReporte> ObtenerCuadroVentasEfectivoAsync(DateOnly fecha, CancellationToken cancellationToken = default);
    Task<BilleteraVirtualReporte> ObtenerBilleteraVirtualAsync(DateOnly fecha, CancellationToken cancellationToken = default);
}

public class CuadroCajaReportProvider : ICuadroCajaReportProvider
{
    private static readonly string[] CategoriasBilletera =
    {
        "TARJETA VISA",
        "TARJETA MASTERCARD",
        "YAPE",
        "PLIN",
        "OTRAS TARJETAS"
    };

    private readonly ICajaVentasDiariasService _cajaVentas;

    public CuadroCajaReportProvider(ICajaVentasDiariasService cajaVentas)
    {
        _cajaVentas = cajaVentas;
    }

    public async Task<CuadroVentasEfectivoReporte> ObtenerCuadroVentasEfectivoAsync(
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var listado = await _cajaVentas.ObtenerListadoDiaAsync(fecha, null, cancellationToken);

        var filas = listado
            .GroupBy(x => new { x.IdAlmacen, x.NombreAlmacen })
            .Select(g => new CuadroVentasEfectivoFila
            {
                Tienda = g.Key.NombreAlmacen,
                VentaDia = g.Sum(x => x.VentaDia),
                EfectivoSaldoCaja = g.Sum(x => x.SaldoFinal)
            })
            .OrderBy(f => f.Tienda, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new CuadroVentasEfectivoReporte
        {
            Fecha = fecha,
            Filas = filas,
            TotalVentaDia = filas.Sum(f => f.VentaDia),
            TotalEfectivo = filas.Sum(f => f.EfectivoSaldoCaja)
        };
    }

    public async Task<BilleteraVirtualReporte> ObtenerBilleteraVirtualAsync(
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var listado = await _cajaVentas.ObtenerListadoDiaAsync(fecha, null, cancellationToken);

        var razonesSociales = listado
            .Select(x => NormalizarRazonSocial(x.RazonSocial))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var montos = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var categoria in CategoriasBilletera)
            montos[categoria] = razonesSociales.ToDictionary(r => r, _ => 0m, StringComparer.OrdinalIgnoreCase);

        foreach (var item in listado)
        {
            var razon = NormalizarRazonSocial(item.RazonSocial);
            if (string.IsNullOrWhiteSpace(razon) || !razonesSociales.Contains(razon))
                continue;

            foreach (var venta in item.Ventas)
            {
                if (!EsPagoBilleteraVirtual(venta.Descripcion))
                    continue;

                if (venta.Estado == "A" && !venta.Descripcion.Contains("(N.C.)", StringComparison.OrdinalIgnoreCase))
                    continue;

                var categoria = CategorizarMetodoPago(venta.Descripcion);
                if (!montos.TryGetValue(categoria, out var porRazon))
                    continue;

                porRazon[razon] += venta.Soles;
            }
        }

        var filas = CategoriasBilletera.Select(categoria =>
        {
            var porRazon = montos[categoria];
            return new BilleteraVirtualFila
            {
                Tipo = categoria,
                MontosPorRazon = porRazon,
                Total = porRazon.Values.Sum()
            };
        }).ToList();

        var totalesPorRazon = razonesSociales.ToDictionary(
            r => r,
            r => filas.Sum(f => f.MontosPorRazon.TryGetValue(r, out var m) ? m : 0m),
            StringComparer.OrdinalIgnoreCase);

        return new BilleteraVirtualReporte
        {
            Fecha = fecha,
            RazonesSociales = razonesSociales,
            Filas = filas,
            TotalesPorRazon = totalesPorRazon,
            TotalGeneral = filas.Sum(f => f.Total)
        };
    }

    private static string NormalizarRazonSocial(string? razonSocial)
    {
        return string.IsNullOrWhiteSpace(razonSocial) ? string.Empty : razonSocial.Trim();
    }

    private static bool EsPagoBilleteraVirtual(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
            return false;

        var baseDescripcion = descripcion.Split(" (N.C.)", StringSplitOptions.None)[0];
        var tipo = baseDescripcion.Split(' ')[0];
        return tipo is "Tarjeta" or "Online";
    }

    internal static string CategorizarMetodoPago(string descripcion)
    {
        var baseDescripcion = descripcion.Split(" (N.C.)", StringSplitOptions.None)[0];
        var upper = baseDescripcion.ToUpperInvariant();

        if (upper.Contains("VISA"))
            return "TARJETA VISA";

        if (upper.Contains("MASTERCARD") || upper.Contains("MASTER CARD") || upper.Contains("MASTERC"))
            return "TARJETA MASTERCARD";

        if (upper.Contains("YAPE"))
            return "YAPE";

        if (upper.Contains("PLIN"))
            return "PLIN";

        return "OTRAS TARJETAS";
    }
}
