using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers;

[Route("api/serie-guia-remision")]
[ApiController]
[Authorize(Roles = "ADMINISTRADOR")]
public class SerieGuiaRemisionController : ControllerBase
{
    private readonly SyscharlesContext _context;

    public SerieGuiaRemisionController(SyscharlesContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Listar todas las series de guías de remisión
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarTodas()
    {
        var lista = await _context.SerieGuiaRemisions
            .Include(s => s.IdAlmacenNavigation)
            .Where(s => s.Estado)
            .Select(s => new
            {
                s.IdSerieGuiaRemision,
                s.IdAlmacen,
                NombreAlmacen = s.IdAlmacenNavigation.Nombre,
                s.Serie,
                s.Correlativo,
                s.Estado,
                s.FechaCreacion,
                s.FechaModificacion
            })
            .OrderBy(s => s.NombreAlmacen)
            .ToListAsync();

        return Ok(lista);
    }

    /// <summary>
    /// Obtener la serie asignada a un almacén específico
    /// </summary>
    [HttpGet("almacen/{idAlmacen}")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerPorAlmacen(int idAlmacen)
    {
        var serie = await _context.SerieGuiaRemisions
            .Include(s => s.IdAlmacenNavigation)
            .Where(s => s.IdAlmacen == idAlmacen && s.Estado)
            .Select(s => new
            {
                s.IdSerieGuiaRemision,
                s.IdAlmacen,
                NombreAlmacen = s.IdAlmacenNavigation.Nombre,
                s.Serie,
                s.Correlativo,
                s.Estado
            })
            .FirstOrDefaultAsync();

        if (serie == null)
            return NotFound(new { mensaje = "Este almacén no tiene serie de guía de remisión asignada" });

        return Ok(serie);
    }

    /// <summary>
    /// Crear nueva serie de guía de remisión para un almacén
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSerieGuiaRemisionRequest dto)
    {
        if (dto == null)
            return BadRequest(new { mensaje = "Datos inválidos" });

        // Validar que el almacén no tenga ya una serie asignada
        var existente = await _context.SerieGuiaRemisions
            .AnyAsync(s => s.IdAlmacen == dto.IdAlmacen && s.Estado);

        if (existente)
            return BadRequest(new { mensaje = "Este almacén ya tiene una serie de guía de remisión asignada" });

        // Validar que la serie no esté en uso
        var serieEnUso = await _context.SerieGuiaRemisions
            .AnyAsync(s => s.Serie == dto.Serie && s.Estado);

        if (serieEnUso)
            return BadRequest(new { mensaje = $"La serie {dto.Serie} ya está asignada a otro almacén" });

        try
        {
            var entidad = new SerieGuiaRemision
            {
                IdAlmacen = dto.IdAlmacen,
                Serie = dto.Serie.ToUpper(),
                Correlativo = dto.Correlativo,
                Estado = true,
                FechaCreacion = DateTime.Now
            };

            _context.SerieGuiaRemisions.Add(entidad);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Serie de guía de remisión creada correctamente", id = entidad.IdSerieGuiaRemision });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = $"Error al crear serie: {ex.Message}" });
        }
    }

    /// <summary>
    /// Actualizar correlativo o estado de una serie
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarSerieGuiaRemisionRequest dto)
    {
        var serie = await _context.SerieGuiaRemisions.FindAsync(id);
        if (serie == null)
            return NotFound(new { mensaje = "Serie no encontrada" });

        try
        {
            serie.Correlativo = dto.Correlativo;
            serie.Estado = dto.Estado;
            serie.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Serie actualizada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = $"Error al actualizar: {ex.Message}" });
        }
    }

    /// <summary>
    /// Desactivar (eliminar lógicamente) una serie
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var serie = await _context.SerieGuiaRemisions.FindAsync(id);
        if (serie == null)
            return NotFound(new { mensaje = "Serie no encontrada" });

        try
        {
            serie.Estado = false;
            serie.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Serie desactivada correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = $"Error al eliminar: {ex.Message}" });
        }
    }

    /// <summary>
    /// Incrementar el correlativo de una serie (uso interno)
    /// </summary>
    [HttpPost("{id}/incrementar")]
    [AllowAnonymous]
    public async Task<IActionResult> IncrementarCorrelativo(int id)
    {
        var serie = await _context.SerieGuiaRemisions.FindAsync(id);
        if (serie == null)
            return NotFound(new { mensaje = "Serie no encontrada" });

        try
        {
            serie.Correlativo++;
            serie.FechaModificacion = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { correlativo = serie.Correlativo });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = $"Error: {ex.Message}" });
        }
    }
}

public class CrearSerieGuiaRemisionRequest
{
    public int IdAlmacen { get; set; }
    public string Serie { get; set; } = string.Empty;
    public int Correlativo { get; set; } = 0;
}

public class ActualizarSerieGuiaRemisionRequest
{
    public int Correlativo { get; set; }
    public bool Estado { get; set; }
}
