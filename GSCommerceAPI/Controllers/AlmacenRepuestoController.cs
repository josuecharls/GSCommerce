using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenRepuestoController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AlmacenRepuestoController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/almacenrepuesto
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlmacenRepuestoDTO>>> GetAlmacenRepuestos()
        {
            var lista = await _context.AlmacenRepuestos
                .Include(ar => ar.IdAlmacenNavigation)
                .Select(ar => new AlmacenRepuestoDTO
                {
                    IdAlmacenRepuesto = ar.IdAlmacenRepuesto,
                    IdAlmacen = ar.IdAlmacen,
                    NombreAlmacen = ar.IdAlmacenNavigation.Nombre,
                    Nombre = ar.Nombre,
                    Direccion = ar.Direccion,
                    Estado = ar.Estado
                })
                .ToListAsync();

            return Ok(lista);
        }

        // GET: api/almacenrepuesto/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AlmacenRepuestoDTO>> GetAlmacenRepuesto(int id)
        {
            var ar = await _context.AlmacenRepuestos
                .Include(x => x.IdAlmacenNavigation)
                .FirstOrDefaultAsync(x => x.IdAlmacenRepuesto == id);

            if (ar == null)
                return NotFound();

            var dto = new AlmacenRepuestoDTO
            {
                IdAlmacenRepuesto = ar.IdAlmacenRepuesto,
                IdAlmacen = ar.IdAlmacen,
                NombreAlmacen = ar.IdAlmacenNavigation.Nombre,
                Nombre = ar.Nombre,
                Direccion = ar.Direccion,
                Estado = ar.Estado
            };

            return Ok(dto);
        }

        // GET: api/almacenrepuesto/poralmacen/5
        [HttpGet("poralmacen/{idAlmacen}")]
        public async Task<ActionResult<IEnumerable<AlmacenRepuestoDTO>>> GetPorAlmacen(int idAlmacen)
        {
            var lista = await _context.AlmacenRepuestos
                .Include(ar => ar.IdAlmacenNavigation)
                .Where(ar => ar.IdAlmacen == idAlmacen)
                .Select(ar => new AlmacenRepuestoDTO
                {
                    IdAlmacenRepuesto = ar.IdAlmacenRepuesto,
                    IdAlmacen = ar.IdAlmacen,
                    NombreAlmacen = ar.IdAlmacenNavigation.Nombre,
                    Nombre = ar.Nombre,
                    Direccion = ar.Direccion,
                    Estado = ar.Estado
                })
                .ToListAsync();

            return Ok(lista);
        }

        // POST: api/almacenrepuesto
        [HttpPost]
        public async Task<ActionResult<AlmacenRepuesto>> PostAlmacenRepuesto(AlmacenRepuestoDTO dto)
        {
            var entity = new AlmacenRepuesto
            {
                IdAlmacen = dto.IdAlmacen,
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Estado = dto.Estado
            };

            _context.AlmacenRepuestos.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAlmacenRepuesto), new { id = entity.IdAlmacenRepuesto }, entity);
        }

        // PUT: api/almacenrepuesto/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAlmacenRepuesto(int id, AlmacenRepuestoDTO dto)
        {
            if (id != dto.IdAlmacenRepuesto)
                return BadRequest();

            var entity = await _context.AlmacenRepuestos.FindAsync(id);
            if (entity == null)
                return NotFound();

            entity.IdAlmacen = dto.IdAlmacen;
            entity.Nombre = dto.Nombre;
            entity.Direccion = dto.Direccion;
            entity.Estado = dto.Estado;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.AlmacenRepuestos.Any(e => e.IdAlmacenRepuesto == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/almacenrepuesto/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlmacenRepuesto(int id)
        {
            var entity = await _context.AlmacenRepuestos.FindAsync(id);
            if (entity == null)
                return NotFound();

            _context.AlmacenRepuestos.Remove(entity);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
