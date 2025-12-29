using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DireccionRucController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public DireccionRucController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/DireccionRuc/5
        [HttpGet("{ruc}")]
        public async Task<ActionResult<List<DireccionRuc>>> GetDireccionesPorRuc(string ruc)
        {
            if (string.IsNullOrEmpty(ruc) || ruc.Length != 11)
            {
                return BadRequest("RUC inválido");
            }

            return await _context.DireccionRucs
                .Where(d => d.Ruc == ruc)
                .OrderByDescending(d => d.EsDefault)
                .ThenByDescending(d => d.FechaCreacion)
                .ToListAsync();
        }

        // POST: api/DireccionRuc
        [HttpPost]
        public async Task<ActionResult<DireccionRuc>> PostDireccionRuc(DireccionRuc direccion)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Si es default, quitar default a otras direcciones del mismo RUC
            if (direccion.EsDefault)
            {
                var otrasDirecciones = await _context.DireccionRucs
                    .Where(d => d.Ruc == direccion.Ruc && d.EsDefault)
                    .ToListAsync();

                foreach (var item in otrasDirecciones)
                {
                    item.EsDefault = false;
                }
            }

            direccion.FechaCreacion = DateTime.Now;
            _context.DireccionRucs.Add(direccion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDireccionesPorRuc", new { ruc = direccion.Ruc }, direccion);
        }

        // PUT: api/DireccionRuc/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDireccionRuc(int id, DireccionRuc direccion)
        {
            if (id != direccion.IdDireccionRuc)
            {
                return BadRequest();
            }

            if (direccion.EsDefault)
            {
                // Quitar default a otras direcciones
                var otrasDirecciones = await _context.DireccionRucs
                    .Where(d => d.Ruc == direccion.Ruc && d.IdDireccionRuc != id && d.EsDefault)
                    .ToListAsync();

                foreach (var item in otrasDirecciones)
                {
                    item.EsDefault = false;
                }
            }

            _context.Entry(direccion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DireccionRucExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/DireccionRuc/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDireccionRuc(int id)
        {
            var direccion = await _context.DireccionRucs.FindAsync(id);
            if (direccion == null)
            {
                return NotFound();
            }

            _context.DireccionRucs.Remove(direccion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DireccionRucExists(int id)
        {
            return _context.DireccionRucs.Any(e => e.IdDireccionRuc == id);
        }
    }
}
