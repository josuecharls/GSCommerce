using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenCuentaController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AlmacenCuentaController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/AlmacenCuenta/5
        [HttpGet("{idAlmacen}")]
        public async Task<ActionResult<IEnumerable<AlmacenCuentaDTO>>> GetCuentas(int idAlmacen)
        {
            var cuentas = await _context.AlmacenCuenta
                .Where(c => c.IdAlmacen == idAlmacen)
                .Select(c => new AlmacenCuentaDTO
                {
                    IdAlmacenCuenta = c.IdAlmacenCuenta,
                    IdAlmacen = c.IdAlmacen,
                    Banco = c.Banco,
                    Cuenta = c.Cuenta,
                    Cci = c.Cci
                })
                .ToListAsync();

            return Ok(cuentas);
        }

        // POST: api/AlmacenCuenta/5
        [HttpPost("{idAlmacen}")]
        public async Task<IActionResult> SaveCuentas(int idAlmacen, List<AlmacenCuentaDTO> cuentas)
        {
            var existentes = await _context.AlmacenCuenta
                .Where(c => c.IdAlmacen == idAlmacen)
                .ToListAsync();
            _context.AlmacenCuenta.RemoveRange(existentes);
            await _context.SaveChangesAsync();

            var nuevas = cuentas.Select(c => new AlmacenCuentum
            {
                IdAlmacen = idAlmacen,
                Banco = c.Banco,
                Cuenta = c.Cuenta,
                Cci = c.Cci
            });
            await _context.AlmacenCuenta.AddRangeAsync(nuevas);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}