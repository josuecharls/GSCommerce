using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesCorrelativosController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        public SeriesCorrelativosController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("almacen/{idAlmacen}")]
        public async Task<IActionResult> ObtenerPorAlmacen(int idAlmacen)
        {
            var lista = await _context.SerieCorrelativos
                .Where(s => s.IdAlmacen == idAlmacen)
                .Include(s => s.IdAlmacenNavigation)
                .Include(s => s.IdTipoDocumentoVentaNavigation)
                .Select(s => new SerieCorrelativoDTO
                {
                    IdSerieCorrelativo = s.IdSerieCorrelativo,
                    IdAlmacen = s.IdAlmacen,
                    IdTipoDocumentoVenta = s.IdTipoDocumentoVenta,
                    Serie = s.Serie,
                    Correlativo = s.Correlativo,
                    Estado = s.Estado,
                    NombreAlmacen = s.IdAlmacenNavigation.Nombre,
                    NombreTipoDocumento = s.IdTipoDocumentoVentaNavigation.Descripcion
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> CrearSerie([FromBody] SerieCorrelativoDTO dto)
        {
            if (dto == null)
                return BadRequest("Datos inválidos.");

            try
            {
                var entidad = new SerieCorrelativo
                {
                    IdAlmacen = dto.IdAlmacen,
                    IdTipoDocumentoVenta = dto.IdTipoDocumentoVenta,
                    Serie = dto.Serie,
                    Correlativo = dto.Correlativo,
                    Estado = dto.Estado
                };

                _context.SerieCorrelativos.Add(entidad);
                await _context.SaveChangesAsync();

                return Ok("Serie registrada correctamente.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error al registrar serie: {ex.Message}");
            }
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> ObtenerSeriePorTipoYAlmacen([FromQuery] string tipoDoc, [FromQuery] int idAlmacen)
        {
            var tipoDocEntity = await _context.TipoDocumentoVenta
                .FirstOrDefaultAsync(t => t.Abreviatura.ToUpper() == tipoDoc.ToUpper());

            if (tipoDocEntity == null)
                return NotFound("Tipo de documento no válido.");

            var serie = await _context.SerieCorrelativos
                .Where(s => s.IdAlmacen == idAlmacen &&
                            s.IdTipoDocumentoVenta == tipoDocEntity.IdTipoDocumentoVenta &&
                            s.Estado)
                .Select(s => s.Serie)
                .FirstOrDefaultAsync();

            return serie != null ? Ok(serie) : NotFound("Serie no encontrada.");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarSerieCorrelativo(int id, SerieCorrelativoDTO dto)
        {
            if (id != dto.IdSerieCorrelativo)
                return BadRequest("ID no coincide");

            // Debido a que SerieCorrelativo posee clave compuesta se deben
            // proporcionar todos los valores al buscar la entidad
            var serie = await _context.SerieCorrelativos.FindAsync(
                id,
                dto.IdAlmacen,
                dto.IdTipoDocumentoVenta,
                dto.Serie);
            if (serie == null)
                return NotFound("No se encontró la serie");

            try
            {
                serie.Correlativo = dto.Correlativo;
                serie.Estado = dto.Estado;
                // Nota: No se permite cambiar Almacén, Tipo de Documento ni Serie

                await _context.SaveChangesAsync();
                return Ok("Serie actualizada correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error actualizando: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarSerieCorrelativo(int id)
        {
            // Buscar la serie por ID. Aunque la clave es compuesta,
            // normalmente IdSerieCorrelativo es único.
            var serie = await _context.SerieCorrelativos
                .FirstOrDefaultAsync(s => s.IdSerieCorrelativo == id);
            if (serie == null)
                return NotFound("No se encontró la serie");

            try
            {
                serie.Estado = false; // Desactivación lógica
                await _context.SaveChangesAsync();
                return Ok("Serie desactivada correctamente");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al eliminar: {ex.Message}");
            }
        }

        [HttpGet("disponibles/{idAlmacen}")]
        public async Task<ActionResult<IEnumerable<VSeriesXalmacen1>>> ObtenerSeriesDisponibles(int idAlmacen)
        {
            var disponibles = await _context.VSeriesXalmacen1s
                .Where(v => v.IdAlmacen == idAlmacen)
                .ToListAsync();

            return Ok(disponibles);
        }
    }
}
