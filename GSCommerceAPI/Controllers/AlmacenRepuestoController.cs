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
[Authorize]
public class AlmacenRepuestoController : ControllerBase
{
    private readonly SyscharlesContext _context;

    public AlmacenRepuestoController(SyscharlesContext context)
    {
        _context = context;
    }

    private string? Cargo => User.FindFirst("Cargo")?.Value;

    private string? UserIdClaim => User.FindFirst("userId")?.Value;

    private static AlmacenRepuestoDTO MapDto(AlmacenRepuesto ar) => new()
    {
        IdAlmacenRepuesto = ar.IdAlmacenRepuesto,
        IdAlmacen = ar.IdAlmacen,
        NombreAlmacen = ar.IdAlmacenNavigation.Nombre,
        Nombre = ar.Nombre,
        Direccion = ar.Direccion,
        Estado = ar.Estado,
        TotalArticulos = null,
        TotalUnidades = null
    };

    [HttpGet]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO")]
    public async Task<ActionResult<IEnumerable<AlmacenRepuestoDTO>>> GetAlmacenRepuestos(CancellationToken ct = default)
    {
        var query = _context.AlmacenRepuestos
            .Include(ar => ar.IdAlmacenNavigation)
            .AsQueryable();

        if (RepuestoAccesoHelper.EsCajero(Cargo))
        {
            var idAlmacen = await RepuestoAccesoHelper.ObtenerIdAlmacenUsuarioAsync(_context, UserIdClaim, ct);
            if (!idAlmacen.HasValue || idAlmacen.Value <= 0)
                return Ok(new List<AlmacenRepuestoDTO>());

            query = query.Where(ar => ar.IdAlmacen == idAlmacen.Value);
        }
        else if (!RepuestoAccesoHelper.EsAdministrador(Cargo))
            return Forbid();

        var lista = await query
            .OrderBy(ar => ar.IdAlmacenNavigation.Nombre)
            .ThenBy(ar => ar.Nombre)
            .ToListAsync(ct);

        var ids = lista.Select(x => x.IdAlmacenRepuesto).ToList();

        var resumen = await _context.StockAlmacenRepuestos
            .AsNoTracking()
            .Where(s => ids.Contains(s.IdAlmacenRepuesto))
            .GroupBy(s => s.IdAlmacenRepuesto)
            .Select(g => new
            {
                IdAlmacenRepuesto = g.Key,
                TotalArticulos = g.Count(),
                TotalUnidades = g.Sum(x => x.Cantidad)
            })
            .ToDictionaryAsync(x => x.IdAlmacenRepuesto, ct);

        var dtos = lista.Select(ar =>
        {
            var dto = MapDto(ar);
            if (resumen.TryGetValue(ar.IdAlmacenRepuesto, out var r))
            {
                dto.TotalArticulos = r.TotalArticulos;
                dto.TotalUnidades = r.TotalUnidades;
            }
            else
            {
                dto.TotalArticulos = 0;
                dto.TotalUnidades = 0;
            }
            return dto;
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO")]
    public async Task<ActionResult<AlmacenRepuestoDTO>> GetAlmacenRepuesto(int id, CancellationToken ct = default)
    {
        if (!await RepuestoAccesoHelper.PuedeUsarAlmacenRepuestoAsync(_context, Cargo, UserIdClaim, id, ct))
            return Forbid();

        var ar = await _context.AlmacenRepuestos
            .Include(x => x.IdAlmacenNavigation)
            .FirstOrDefaultAsync(x => x.IdAlmacenRepuesto == id, ct);

        if (ar == null)
            return NotFound();

        return Ok(MapDto(ar));
    }

    [HttpGet("poralmacen/{idAlmacen:int}")]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO")]
    public async Task<ActionResult<IEnumerable<AlmacenRepuestoDTO>>> GetPorAlmacen(int idAlmacen, CancellationToken ct = default)
    {
        if (!await RepuestoAccesoHelper.PuedeVerAlmacenAsync(_context, Cargo, UserIdClaim, idAlmacen, ct))
            return Forbid();

        var lista = await _context.AlmacenRepuestos
            .Include(ar => ar.IdAlmacenNavigation)
            .Where(ar => ar.IdAlmacen == idAlmacen)
            .OrderBy(ar => ar.Nombre)
            .ToListAsync(ct);

        var ids = lista.Select(x => x.IdAlmacenRepuesto).ToList();

        var resumen = await _context.StockAlmacenRepuestos
            .AsNoTracking()
            .Where(s => ids.Contains(s.IdAlmacenRepuesto))
            .GroupBy(s => s.IdAlmacenRepuesto)
            .Select(g => new
            {
                IdAlmacenRepuesto = g.Key,
                TotalArticulos = g.Count(),
                TotalUnidades = g.Sum(x => x.Cantidad)
            })
            .ToDictionaryAsync(x => x.IdAlmacenRepuesto, ct);

        var dtos = lista.Select(ar =>
        {
            var dto = MapDto(ar);
            if (resumen.TryGetValue(ar.IdAlmacenRepuesto, out var r))
            {
                dto.TotalArticulos = r.TotalArticulos;
                dto.TotalUnidades = r.TotalUnidades;
            }
            else
            {
                dto.TotalArticulos = 0;
                dto.TotalUnidades = 0;
            }
            return dto;
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<ActionResult<AlmacenRepuesto>> PostAlmacenRepuesto(AlmacenRepuestoDTO dto, CancellationToken ct = default)
    {
        if (dto.IdAlmacen <= 0 || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest("Tienda y nombre son requeridos.");

        var entity = new AlmacenRepuesto
        {
            IdAlmacen = dto.IdAlmacen,
            Nombre = dto.Nombre.Trim(),
            Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim(),
            Estado = dto.Estado
        };

        _context.AlmacenRepuestos.Add(entity);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAlmacenRepuesto), new { id = entity.IdAlmacenRepuesto }, entity);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> PutAlmacenRepuesto(int id, AlmacenRepuestoDTO dto, CancellationToken ct = default)
    {
        if (id != dto.IdAlmacenRepuesto)
            return BadRequest();

        var entity = await _context.AlmacenRepuestos.FindAsync(new object[] { id }, ct);
        if (entity == null)
            return NotFound();

        entity.IdAlmacen = dto.IdAlmacen;
        entity.Nombre = dto.Nombre.Trim();
        entity.Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim();
        entity.Estado = dto.Estado;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.AlmacenRepuestos.AnyAsync(e => e.IdAlmacenRepuesto == id, ct))
                return NotFound();
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMINISTRADOR")]
    public async Task<IActionResult> DeleteAlmacenRepuesto(int id, CancellationToken ct = default)
    {
        var entity = await _context.AlmacenRepuestos.FindAsync(new object[] { id }, ct);
        if (entity == null)
            return NotFound();

        var tieneStock = await _context.StockAlmacenRepuestos
            .AnyAsync(s => s.IdAlmacenRepuesto == id, ct);

        if (tieneStock)
            return BadRequest("No se puede eliminar el sótano mientras tenga artículos con cantidad mayor a cero.");

        _context.AlmacenRepuestos.Remove(entity);
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
