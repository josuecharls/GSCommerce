using GSCommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Helpers;

/// <summary>
/// El stock sin variante se guarda con <c>IdVariante = NULL</c>. El cliente o JSON pueden mandar <c>0</c>,
/// lo que hacía fallar los <c>FirstOrDefault</c> con <c>==</c> y duplicaba filas de stock.
/// </summary>
public static class IdVarianteUtil
{
    public static int? Normalizar(int? id) => id is null or 0 ? null : id;

    public static async Task<StockAlmacen?> BuscarStockAlmacenAsync(
        DbSet<StockAlmacen> set,
        int idAlmacen,
        string idArticulo,
        int? idVariante,
        CancellationToken ct = default)
    {
        var idArt = (idArticulo ?? string.Empty).Trim();
        var nv = Normalizar(idVariante);
        if (nv == null)
        {
            return await set.FirstOrDefaultAsync(
                s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArt && s.IdVariante == null,
                ct);
        }

        return await set.FirstOrDefaultAsync(
            s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArt && s.IdVariante == nv,
            ct);
    }

    public static StockAlmacen? BuscarStockAlmacenLocal(
        IEnumerable<StockAlmacen> local,
        int idAlmacen,
        string idArticulo,
        int? idVariante)
    {
        var idArt = (idArticulo ?? string.Empty).Trim();
        var nv = Normalizar(idVariante);
        return local.FirstOrDefault(s =>
            s.IdAlmacen == idAlmacen && s.IdArticulo == idArt &&
            (nv == null ? s.IdVariante == null : s.IdVariante == nv));
    }

    /// <summary>
    /// Resuelve la fila de <see cref="StockAlmacen"/> para una línea de venta.
    /// Cubre: (1) fila exacta por variante con saldo suficiente; (2) solo fila <c>NULL</c> con variante en el detalle;
    /// (3) fila de variante con 0 (o saldo menor a lo pedido) y una única fila <c>NULL</c> que sí alcanza (migración parcial);
    /// (4) línea sin variante y una sola fila de stock.
    /// </summary>
    public static async Task<(StockAlmacen Fila, int? IdVarianteMovimiento)?> ResolverStockLineaVentaAsync(
        DbSet<StockAlmacen> set,
        int idAlmacen,
        string idArticulo,
        int? idVarianteSolicitado,
        decimal cantidadRequerida,
        CancellationToken ct = default)
    {
        var nv = Normalizar(idVarianteSolicitado);
        var idArt = (idArticulo ?? string.Empty).Trim();

        var stockExact = await BuscarStockAlmacenAsync(set, idAlmacen, idArticulo, idVarianteSolicitado, ct);
        if (stockExact != null && (decimal)stockExact.Stock >= cantidadRequerida)
            return (stockExact, nv);

        var filas = await set
            .Where(s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArt)
            .ToListAsync(ct);

        if (nv != null)
        {
            var nullRows = filas.Where(s => Normalizar(s.IdVariante) == null).ToList();
            if (nullRows.Count == 1 && (decimal)nullRows[0].Stock >= cantidadRequerida)
            {
                if (stockExact == null || (decimal)stockExact.Stock < cantidadRequerida)
                    return (nullRows[0], nv);
            }

            return null;
        }

        // Línea sin variante explícita: una sola fila que alcanza la cantidad
        if (filas.Count != 1)
            return null;

        var unica = filas[0];
        if ((decimal)unica.Stock < cantidadRequerida)
            return null;

        return (unica, Normalizar(unica.IdVariante));
    }
}
