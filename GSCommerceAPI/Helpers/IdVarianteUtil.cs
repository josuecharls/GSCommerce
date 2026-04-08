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
        var nv = Normalizar(idVariante);
        if (nv == null)
        {
            return await set.FirstOrDefaultAsync(
                s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArticulo && s.IdVariante == null,
                ct);
        }

        return await set.FirstOrDefaultAsync(
            s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArticulo && s.IdVariante == nv,
            ct);
    }

    public static StockAlmacen? BuscarStockAlmacenLocal(
        IEnumerable<StockAlmacen> local,
        int idAlmacen,
        string idArticulo,
        int? idVariante)
    {
        var nv = Normalizar(idVariante);
        return local.FirstOrDefault(s =>
            s.IdAlmacen == idAlmacen && s.IdArticulo == idArticulo &&
            (nv == null ? s.IdVariante == null : s.IdVariante == nv));
    }
}
