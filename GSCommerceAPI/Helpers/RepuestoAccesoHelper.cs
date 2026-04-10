using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Helpers;

public static class RepuestoAccesoHelper
{
    public static bool EsAdministrador(string? cargo) =>
        string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase);

    public static bool EsCajero(string? cargo) =>
        string.Equals(cargo, "CAJERO", StringComparison.OrdinalIgnoreCase);

    public static async Task<int?> ObtenerIdAlmacenUsuarioAsync(
        SyscharlesContext context,
        string? userIdClaim,
        CancellationToken ct = default)
    {
        if (!int.TryParse(userIdClaim, out var userId))
            return null;

        return await context.Usuarios
            .AsNoTracking()
            .Include(u => u.IdPersonalNavigation)
            .Where(u => u.IdUsuario == userId)
            .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
            .FirstOrDefaultAsync(ct);
    }

    public static async Task<bool> PuedeVerAlmacenAsync(
        SyscharlesContext context,
        string? cargo,
        string? userIdClaim,
        int idAlmacen,
        CancellationToken ct = default)
    {
        if (EsAdministrador(cargo))
            return true;

        if (EsCajero(cargo))
        {
            var id = await ObtenerIdAlmacenUsuarioAsync(context, userIdClaim, ct);
            return id.HasValue && id.Value == idAlmacen;
        }

        return false;
    }

    public static async Task<bool> PuedeUsarAlmacenRepuestoAsync(
        SyscharlesContext context,
        string? cargo,
        string? userIdClaim,
        int idAlmacenRepuesto,
        CancellationToken ct = default)
    {
        var idAlmacen = await context.AlmacenRepuestos
            .AsNoTracking()
            .Where(ar => ar.IdAlmacenRepuesto == idAlmacenRepuesto)
            .Select(ar => (int?)ar.IdAlmacen)
            .FirstOrDefaultAsync(ct);

        if (!idAlmacen.HasValue)
            return false;

        return await PuedeVerAlmacenAsync(context, cargo, userIdClaim, idAlmacen.Value, ct);
    }
}
