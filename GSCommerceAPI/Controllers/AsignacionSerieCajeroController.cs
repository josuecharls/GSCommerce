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
            var permitidos = new[] { "BOLETA", "FACTURA", "TICKET", "BOLETA M", "FACTURA M" };

            var asignadas = await _context.AsignacionSerieCajeros
                .Select(a => a.IdSerieCorrelativo)
                .ToListAsync();

            var series = await (from sc in _context.SerieCorrelativos
                                join td in _context.TipoDocumentoVenta
                                    on sc.IdTipoDocumentoVenta equals td.IdTipoDocumentoVenta
                                where sc.IdAlmacen == idAlmacen
                                      && sc.Estado
                                      && permitidos.Contains(td.Descripcion)
                                      && !asignadas.Contains(sc.IdSerieCorrelativo)
                                select new VSeriesXalmacen1
                                {
                                    IdSerieCorrelativo = sc.IdSerieCorrelativo,
                                    IdAlmacen = sc.IdAlmacen,
                                    DocumentoSerie = td.Descripcion + " (" + sc.Serie + ")"
                                }).ToListAsync();

            return Ok(series);
        }

        [HttpGet("asignadas")]
        public async Task<IActionResult> GetSeriesAsignadas([FromQuery] int idUsuario, [FromQuery] int idAlmacen)
        {
            var permitidos = new[] { "BOLETA", "FACTURA", "TICKET", "BOLETA M", "FACTURA M" };

            var series = await (from a in _context.AsignacionSerieCajeros
                                join sc in _context.SerieCorrelativos
                                    on a.IdSerieCorrelativo equals sc.IdSerieCorrelativo
                                join td in _context.TipoDocumentoVenta
                                    on sc.IdTipoDocumentoVenta equals td.IdTipoDocumentoVenta
                                where a.IdUsuario == idUsuario
                                      && a.IdAlmacen == idAlmacen
                                      && sc.Estado
                                      && permitidos.Contains(td.Descripcion)
                                select new VSeriesXcajero1
                                {
                                    IdSerieCorrelativo = sc.IdSerieCorrelativo,
                                    IdUsuario = a.IdUsuario,
                                    IdAlmacen = a.IdAlmacen,
                                    DocumentoSerie = td.Descripcion + " (" + sc.Serie + ")"
                                }).ToListAsync();

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
