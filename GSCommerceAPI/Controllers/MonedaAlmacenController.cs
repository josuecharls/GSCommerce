using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MonedaAlmacenController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private const string Prefix = "MonedaAlmacen_";

        public MonedaAlmacenController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MonedaAlmacenDTO>> Obtener(int id)
        {
            var key = $"{Prefix}{id}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);
            var moneda = config?.Valor ?? "PEN";
            return Ok(new MonedaAlmacenDTO { IdAlmacen = id, Moneda = moneda });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, MonedaAlmacenDTO dto)
        {
            if (id != dto.IdAlmacen)
                return BadRequest();

            var key = $"{Prefix}{id}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);
            if (config == null)
            {
                config = new Configuracion { Configuracion1 = key, Valor = dto.Moneda, Descripcion = "Moneda del almacen" };
                _context.Configuracions.Add(config);
            }
            else
            {
                config.Valor = dto.Moneda;
                _context.Configuracions.Update(config);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}