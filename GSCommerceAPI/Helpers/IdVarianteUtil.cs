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
    /// Incluye: (1) línea sin variante pero una sola fila de stock; (2) línea con variante sin fila propia
    /// pero una única fila con <c>IdVariante NULL</c> (inventario cargado antes de usar variantes por almacén).
    /// </summary>
    public static async Task<(StockAlmacen Fila, int? IdVarianteMovimiento)?> ResolverStockLineaVentaAsync(
        DbSet<StockAlmacen> set,
        int idAlmacen,
        string idArticulo,
        int? idVarianteSolicitado,
        CancellationToken ct = default)
    {
        var nv = Normalizar(idVarianteSolicitado);
        var stock = await BuscarStockAlmacenAsync(set, idAlmacen, idArticulo, idVarianteSolicitado, ct);
        if (stock != null)
            return (stock, nv);

        var idArt = (idArticulo ?? string.Empty).Trim();
        var filas = await set
            .Where(s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArt)
            .ToListAsync(ct);

        // Venta con talla/color (IdVariante en el detalle) pero el stock sigue en una sola fila sin variante
        if (nv != null && filas.Count == 1 && Normalizar(filas[0].IdVariante) == null)
            return (filas[0], nv);

        if (nv != null)
            return null;

        if (filas.Count != 1)
            return null;

        var unica = filas[0];
        return (unica, Normalizar(unica.IdVariante));
    }
}
