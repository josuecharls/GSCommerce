using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DescuentosController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        public DescuentosController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("{idAlmacen}")]
        public async Task<IActionResult> ObtenerDescuentos(int idAlmacen)
        {
            var lista = await _context.VDescuentos
                .Where(d => d.IdAlmacen == idAlmacen)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("{idAlmacen}/articulo/{idArticulo}")]
        public async Task<IActionResult> ObtenerDescuentoArticulo(int idAlmacen, string idArticulo)
        {
            if (!int.TryParse(idArticulo, out var idArt))
                return Ok(0);

            var desc = await _context.Descuentos
                .FirstOrDefaultAsync(d => d.IdAlmacen == idAlmacen && d.IdArticulo == idArt);
            if (desc == null)
                return Ok(0);
            return Ok(desc.Descuento1);
        }

        [HttpPost]
        public async Task<IActionResult> CrearDescuento([FromBody] Descuento dto)
        {
            try
            {
                _context.Descuentos.Add(dto);
                await _context.SaveChangesAsync();

                Console.WriteLine($"🔧 Nuevo descuento ID generado: {dto.IdDescuento}");
                // Devuelve el DTO completo con ID generado
                return Ok(new DescuentoDTO
                {
                    IdDescuento = dto.IdDescuento,
                    IdAlmacen = dto.IdAlmacen,
                    IdArticulo = dto.IdArticulo,
                    Descuento = dto.Descuento1,
                    DescuentoPorc = dto.Descuento1
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al crear el descuento {ex.Message} ");
                return StatusCode(500, "Error al crear el descuento");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ModificarDescuento(int id, [FromBody] DescuentoDTO dto)
        {
            var existente = await _context.Descuentos.FindAsync(id);
            if (existente == null)
                return NotFound();

            existente.Descuento1 = dto.DescuentoPorc; // Aquí está la clave
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarDescuento(int id)
        {
            var existente = await _context.Descuentos.FindAsync(id);
            if (existente == null)
                return NotFound();
            _context.Descuentos.Remove(existente);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}