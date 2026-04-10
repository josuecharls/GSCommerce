using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Helpers;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMINISTRADOR,CAJERO")]
public class StockAlmacenRepuestoController : ControllerBase
{
    private readonly SyscharlesContext _context;

    public StockAlmacenRepuestoController(SyscharlesContext context)
    {
        _context = context;
    }

    private string? Cargo => User.FindFirst("Cargo")?.Value;

    private string? UserIdClaim => User.FindFirst("userId")?.Value;

    /// <summary>Listado de artículos y cantidades en un sótano (solo activos con cantidad &gt; 0).</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StockAlmacenRepuestoLineaDTO>>> GetStock(
        [FromQuery] int idAlmacenRepuesto,
        CancellationToken ct = default)
    {
        if (idAlmacenRepuesto <= 0)
            return BadRequest("idAlmacenRepuesto es requerido.");

        if (!await RepuestoAccesoHelper.PuedeUsarAlmacenRepuestoAsync(
                _context, Cargo, UserIdClaim, idAlmacenRepuesto, ct))
            return Forbid();

        var repuesto = await _context.AlmacenRepuestos
            .AsNoTracking()
            .FirstOrDefaultAsync(ar => ar.IdAlmacenRepuesto == idAlmacenRepuesto, ct);

        if (repuesto == null || !repuesto.Estado)
            return NotFound("Sótano no encontrado o inactivo.");

        var lineas = await _context.StockAlmacenRepuestos
            .AsNoTracking()
            .Where(s => s.IdAlmacenRepuesto == idAlmacenRepuesto && s.Cantidad > 0)
            .Join(_context.Articulos.AsNoTracking(),
                s => s.IdArticulo,
                a => a.IdArticulo,
                (s, a) => new StockAlmacenRepuestoLineaDTO
                {
                    IdStockAlmacenRepuesto = s.IdStockAlmacenRepuesto,
                    IdAlmacenRepuesto = s.IdAlmacenRepuesto,
                    IdArticulo = s.IdArticulo,
                    Descripcion = a.Descripcion,
                    Cantidad = s.Cantidad
                })
            .OrderBy(x => x.Descripcion)
            .ToListAsync(ct);

        return Ok(lineas);
    }

    [HttpPost("ajuste")]
    public async Task<IActionResult> Ajustar([FromBody] AjusteStockRepuestoRequest dto, CancellationToken ct = default)
    {
        if (dto.IdAlmacenRepuesto <= 0 || dto.Cantidad <= 0)
            return BadRequest("Sótano y cantidad válidos son requeridos.");

        var tipo = (dto.Tipo ?? "I").Trim().ToUpperInvariant();
        if (tipo is not ("I" or "E"))
            return BadRequest("Tipo debe ser I (ingreso) o E (egreso).");

        if (!await RepuestoAccesoHelper.PuedeUsarAlmacenRepuestoAsync(
                _context, Cargo, UserIdClaim, dto.IdAlmacenRepuesto, ct))
            return Forbid();

        var repuesto = await _context.AlmacenRepuestos
            .FirstOrDefaultAsync(ar => ar.IdAlmacenRepuesto == dto.IdAlmacenRepuesto, ct);

        if (repuesto == null || !repuesto.Estado)
            return NotFound("Sótano no encontrado o inactivo.");

        var idArt = CodigoArticuloUtil.Normalizar(dto.IdArticulo);
        if (idArt.Length == 0)
            return BadRequest("Código de artículo inválido.");

        var existeArticulo = await _context.Articulos.AnyAsync(a => a.IdArticulo == idArt, ct);
        if (!existeArticulo)
            return NotFound($"No existe el artículo {idArt}.");

        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var fila = await _context.StockAlmacenRepuestos
                .FirstOrDefaultAsync(
                    s => s.IdAlmacenRepuesto == dto.IdAlmacenRepuesto && s.IdArticulo == idArt,
                    ct);

            if (tipo == "E")
            {
                if (fila == null || fila.Cantidad < dto.Cantidad)
                {
                    await tx.RollbackAsync(ct);
                    return BadRequest("No hay suficiente cantidad en el sótano para este retiro.");
                }

                fila.Cantidad -= dto.Cantidad;
                if (fila.Cantidad == 0)
                    _context.StockAlmacenRepuestos.Remove(fila);
            }
            else
            {
                // Regla: la suma en todos los sótanos del almacén no puede exceder el stock de tienda.
                var stockTienda = await _context.StockAlmacens
                    .AsNoTracking()
                    .Where(s => s.IdAlmacen == repuesto.IdAlmacen && s.IdArticulo == idArt)
                    .Select(s => (int?)s.Stock)
                    .FirstOrDefaultAsync(ct) ?? 0;

                var totalEnSotanos = await _context.StockAlmacenRepuestos
                    .AsNoTracking()
                    .Where(s => s.IdArticulo == idArt)
                    .Join(
                        _context.AlmacenRepuestos.AsNoTracking().Where(ar => ar.IdAlmacen == repuesto.IdAlmacen),
                        s => s.IdAlmacenRepuesto,
                        ar => ar.IdAlmacenRepuesto,
                        (s, _) => s.Cantidad)
                    .SumAsync(ct);

                var disponible = Math.Max(0, stockTienda - totalEnSotanos);
                if (dto.Cantidad > disponible)
                {
                    await tx.RollbackAsync(ct);
                    return BadRequest(
                        $"Sobrepasaste el límite del stock de la tienda. Disponible para guardar en sótanos: {disponible}. Stock tienda: {stockTienda}.");
                }

                if (fila == null)
                {
                    fila = new StockAlmacenRepuesto
                    {
                        IdAlmacenRepuesto = dto.IdAlmacenRepuesto,
                        IdArticulo = idArt,
                        Cantidad = dto.Cantidad
                    };
                    _context.StockAlmacenRepuestos.Add(fila);
                }
                else
                    fila.Cantidad += dto.Cantidad;
            }

            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
