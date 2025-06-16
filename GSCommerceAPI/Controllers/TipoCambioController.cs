using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoCambioController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public TipoCambioController(SyscharlesContext context)
        {
            _context = context;
        }
        // GET: api/TipoCambio
        [HttpGet("hoy")]
        public async Task<ActionResult<TipoCambioDTO>> ObtenerTipoCambioHoy()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var tc = await _context.TipoDeCambios
                .Where(t => t.Fecha == hoy)
                .FirstOrDefaultAsync();

            if (tc == null)
                return NotFound();

            return Ok(new TipoCambioDTO
            {
                Fecha = tc.Fecha,
                Compra = tc.Compra,
                Venta = tc.Venta
            });
        }

        [HttpGet("mes/{mes:int}/{anio:int}")]
        public async Task<ActionResult<IEnumerable<TipoCambioDTO>>> ObtenerPorMes(int mes, int anio)
        {
            var lista = await _context.TipoDeCambios
                .Where(tc => tc.Fecha.Month == mes && tc.Fecha.Year == anio)
                .OrderBy(tc => tc.Fecha)
                .Select(tc => new TipoCambioDTO
                {
                    Fecha = tc.Fecha,
                    Compra = tc.Compra,
                    Venta = tc.Venta
                })
                .ToListAsync();

            return lista;
        }

        // ✅ POST: api/tipocambio
        [HttpPost]
        public async Task<ActionResult> Insertar(TipoCambioDTO dto)
        {
            var existe = await _context.TipoDeCambios.AnyAsync(tc => tc.Fecha == dto.Fecha);
            if (existe)
                return Conflict("Ya existe tipo de cambio para esa fecha.");

            var nuevo = new TipoDeCambio
            {
                Fecha = dto.Fecha,
                Compra = dto.Compra,
                Venta = dto.Venta
            };

            _context.TipoDeCambios.Add(nuevo);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ✅ PUT: api/tipocambio
        [HttpPut]
        public async Task<ActionResult> Modificar(TipoCambioDTO dto)
        {
            var entidad = await _context.TipoDeCambios.FirstOrDefaultAsync(tc => tc.Fecha == dto.Fecha);
            if (entidad == null)
                return NotFound("No se encontró tipo de cambio para esa fecha.");

            entidad.Compra = dto.Compra;
            entidad.Venta = dto.Venta;

            _context.TipoDeCambios.Update(entidad);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ✅ DELETE: api/tipocambio/2025-06-16
        [HttpDelete("{fecha:datetime}")]
        public async Task<ActionResult> Eliminar(DateTime fecha)
        {
            var fechaOnly = DateOnly.FromDateTime(fecha);
            var entidad = await _context.TipoDeCambios.FirstOrDefaultAsync(tc => tc.Fecha == fechaOnly);
            if (entidad == null)
                return NotFound();

            _context.TipoDeCambios.Remove(entidad);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
