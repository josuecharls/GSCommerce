using GSCommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Helpers;

/// <summary>
/// Stock en <see cref="StockAlmacen"/> es por <c>(IdAlmacen, IdArticulo)</c> sin variante.
/// La variante sigue en comprobantes, guías y kardex para trazabilidad del detalle.
/// </summary>
public static class IdVarianteUtil
{
    public static int? Normalizar(int? id) => id is null or 0 ? null : id;

    public static async Task<StockAlmacen?> BuscarStockAlmacenAsync(
        DbSet<StockAlmacen> set,
        int idAlmacen,
        string idArticulo,
        CancellationToken ct = default)
    {
        var idArt = (idArticulo ?? string.Empty).Trim();
        return await set.FirstOrDefaultAsync(
            s => s.IdAlmacen == idAlmacen && s.IdArticulo == idArt,
            ct);
    }

    public static StockAlmacen? BuscarStockAlmacenLocal(
        IEnumerable<StockAlmacen> local,
        int idAlmacen,
        string idArticulo)
    {
        var idArt = (idArticulo ?? string.Empty).Trim();
        return local.FirstOrDefault(s =>
            s.IdAlmacen == idAlmacen && s.IdArticulo == idArt);
    }

    /// <summary>
    /// Fila de stock del artículo en el almacén; <see cref="IdVarianteMovimiento"/> es el de la línea de venta (comprobante/kardex).
    /// Si <paramref name="cantidadRequerida"/> tiene valor, exige que el saldo sea mayor o igual.
    /// </summary>
    public static async Task<(StockAlmacen Fila, int? IdVarianteMovimiento)?> ResolverStockLineaVentaAsync(
        DbSet<StockAlmacen> set,
        int idAlmacen,
        string idArticulo,
        int? idVarianteSolicitado,
        decimal? cantidadRequerida = null,
        CancellationToken ct = default)
    {
        var nv = Normalizar(idVarianteSolicitado);
        var stock = await BuscarStockAlmacenAsync(set, idAlmacen, idArticulo, ct);
        if (stock == null)
            return null;
        if (cantidadRequerida.HasValue && (decimal)stock.Stock < cantidadRequerida.Value)
            return null;
        return (stock, nv);
    }
}
