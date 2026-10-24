using System.Text.RegularExpressions;
using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Helpers;

/// <summary>
/// Arma / completa la descripción de kardex en transferencias:
/// "TRANSFERENCIA INGRESO Nro: 11525 (CHICLAYO3)".
/// </summary>
public static class KardexTransferenciaDescripcion
{
    private const int MaxOrigen = 100;

    private static readonly Regex IngresoSinTienda = new(
        @"^(?<pref>TRANSFERENCIA INGRESO Nro:?\s*)(?<nro>\d+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string FormatearIngreso(int idMovimiento, string? nombreTiendaOrigen)
    {
        var tienda = (nombreTiendaOrigen ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tienda))
            return Truncar($"TRANSFERENCIA INGRESO Nro: {idMovimiento}");

        return Truncar($"TRANSFERENCIA INGRESO Nro: {idMovimiento} ({tienda})");
    }

    public static string FormatearEgreso(int idMovimiento, string? nombreTiendaDestino)
    {
        var tienda = (nombreTiendaDestino ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tienda))
            return Truncar($"TRANSFERENCIA EGRESO Nro: {idMovimiento}");

        return Truncar($"TRANSFERENCIA EGRESO Nro: {idMovimiento} ({tienda})");
    }

    /// <summary>
    /// Si Operacion es "TRANSFERENCIA INGRESO Nro: X" sin tienda, la completa desde la guía.
    /// </summary>
    public static async Task CompletarTiendaOrigenAsync(
        SyscharlesContext db,
        IList<string?> operaciones,
        CancellationToken ct = default)
    {
        var pendientes = new List<(int Index, int IdMovimiento)>();
        for (var i = 0; i < operaciones.Count; i++)
        {
            var op = operaciones[i]?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(op) || op.Contains('(', StringComparison.Ordinal))
                continue;

            var m = IngresoSinTienda.Match(op);
            if (!m.Success)
                continue;

            if (int.TryParse(m.Groups["nro"].Value, out var idMov))
                pendientes.Add((i, idMov));
        }

        if (pendientes.Count == 0)
            return;

        var ids = pendientes.Select(p => p.IdMovimiento).Distinct().ToList();

        // En TRANSFERENCIA INGRESO, IdAlmacenDestinoOrigen = almacén de donde viene
        var nombres = await (
            from mov in db.MovimientosCabeceras.AsNoTracking()
            where ids.Contains(mov.IdMovimiento)
            join a in db.Almacens.AsNoTracking()
                on mov.IdAlmacenDestinoOrigen equals a.IdAlmacen
            select new { mov.IdMovimiento, a.Nombre }
        ).ToDictionaryAsync(x => x.IdMovimiento, x => x.Nombre, ct);

        // Fallback: espejo egreso → su IdAlmacen es el origen
        var faltantes = ids.Where(id => !nombres.ContainsKey(id)).ToList();
        if (faltantes.Count > 0)
        {
            var porEspejo = await (
                from mov in db.MovimientosCabeceras.AsNoTracking()
                where faltantes.Contains(mov.IdMovimiento) && mov.IdMovimientoEspejo != null
                join eg in db.MovimientosCabeceras.AsNoTracking()
                    on mov.IdMovimientoEspejo equals eg.IdMovimiento
                join a in db.Almacens.AsNoTracking()
                    on eg.IdAlmacen equals a.IdAlmacen
                select new { mov.IdMovimiento, a.Nombre }
            ).ToListAsync(ct);

            foreach (var x in porEspejo)
            {
                if (!nombres.ContainsKey(x.IdMovimiento) && !string.IsNullOrWhiteSpace(x.Nombre))
                    nombres[x.IdMovimiento] = x.Nombre;
            }
        }

        foreach (var (index, idMov) in pendientes)
        {
            if (!nombres.TryGetValue(idMov, out var nombre) || string.IsNullOrWhiteSpace(nombre))
                continue;

            operaciones[index] = FormatearIngreso(idMov, nombre);
        }
    }

    private static string Truncar(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxOrigen)
            return value;
        return value[..MaxOrigen];
    }
}
