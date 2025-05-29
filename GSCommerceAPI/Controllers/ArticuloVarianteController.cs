using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Models;
using GSCommerceAPI.Data;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArticuloVarianteController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public ArticuloVarianteController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/ArticuloVariante/articulo/001001
        [HttpGet("articulo/{idArticulo}")]
        public async Task<ActionResult<IEnumerable<ArticuloVariante>>> GetVariantesPorArticulo(string idArticulo)
        {
            var variantes = await _context.ArticuloVariantes
                .Where(v => v.IdArticulo == idArticulo)
                .ToListAsync();

            return Ok(variantes);
        }

        // POST: api/ArticuloVariante
        [HttpPost]
        public async Task<ActionResult<ArticuloVariante>> PostVariante(ArticuloVariante variante)
        {
            _context.ArticuloVariantes.Add(variante);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVariantesPorArticulo), new { idArticulo = variante.IdArticulo }, variante);
        }

        // DELETE: api/ArticuloVariante/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVariante(int id)
        {
            var variante = await _context.ArticuloVariantes.FindAsync(id);
            if (variante == null)
                return NotFound();

            _context.ArticuloVariantes.Remove(variante);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
