using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IgvAlmacenController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private const string Prefix = "IgvAlmacen_";

        public IgvAlmacenController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IgvAlmacenDTO>> Obtener(int id)
        {
            var key = $"{Prefix}{id}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);
            decimal igv = 18m;
            if (config != null && decimal.TryParse(config.Valor, out var val))
                igv = val;
            return Ok(new IgvAlmacenDTO { IdAlmacen = id, Igv = igv });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, IgvAlmacenDTO dto)
        {
            if (id != dto.IdAlmacen)
                return BadRequest();

            var key = $"{Prefix}{id}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);
            if (config == null)
            {
                config = new Configuracion { Configuracion1 = key, Valor = dto.Igv.ToString(), Descripcion = "IGV del almacen" };
                _context.Configuracions.Add(config);
            }
            else
            {
                config.Valor = dto.Igv.ToString();
                _context.Configuracions.Update(config);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}