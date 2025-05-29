using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsignacionSerieCajeroController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AsignacionSerieCajeroController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("disponibles")]
        public async Task<IActionResult> GetSeriesDisponibles([FromQuery] int idAlmacen)
        {
            var series = await _context.VSeriesXalmacen1s
                .Where(x => x.IdAlmacen == idAlmacen)
                .ToListAsync();

            return Ok(series);
        }

        [HttpGet("asignadas")]
        public async Task<IActionResult> GetSeriesAsignadas([FromQuery] int idUsuario, [FromQuery] int idAlmacen)
        {
            var series = await _context.VSeriesXcajero1s
                .Where(x => x.IdUsuario == idUsuario && x.IdAlmacen == idAlmacen)
                .ToListAsync();

            return Ok(series);
        }

        [HttpPost("guardar")]
        public async Task<IActionResult> GuardarAsignacion([FromBody] AsignacionSerieCajeroDTO dto)
        {
            var existentes = await _context.AsignacionSerieCajeros
                .Where(x => x.IdUsuario == dto.IdUsuario && x.IdAlmacen == dto.IdAlmacen)
                .ToListAsync();

            _context.AsignacionSerieCajeros.RemoveRange(existentes);

            foreach (var idSerie in dto.IdSeries)
            {
                _context.AsignacionSerieCajeros.Add(new AsignacionSerieCajero
                {
                    IdUsuario = dto.IdUsuario,
                    IdAlmacen = dto.IdAlmacen,
                    IdSerieCorrelativo = idSerie
                });
            }

            await _context.SaveChangesAsync();
            return Ok("Series asignadas correctamente.");
        }

    }

}
