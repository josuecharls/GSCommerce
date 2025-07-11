using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreciosAlmacenController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        public PreciosAlmacenController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("{idAlmacen}")]
        public async Task<ActionResult<IEnumerable<VDescuento>>> GetDescuentos(int idAlmacen)
        {
            var lista = await _context.VDescuentos
                .Where(d => d.IdAlmacen == idAlmacen)
                .ToListAsync();
            return Ok(lista);
        }

        [HttpGet("{idAlmacen}/{idArticulo}")]
        public async Task<ActionResult<VDescuento>> GetPrecio(int idAlmacen, string idArticulo)
        {
            var desc = await _context.VDescuentos
                .FirstOrDefaultAsync(d => d.IdAlmacen == idAlmacen && d.IdArticulo == idArticulo);
            if (desc != null)
                return Ok(desc);

            var art = await _context.Articulos.FirstOrDefaultAsync(a => a.IdArticulo == idArticulo);
            if (art == null)
                return NotFound();

            return Ok(new VDescuento
            {
                IdAlmacen = idAlmacen,
                IdArticulo = art.IdArticulo,
                DescripcionCorta = art.DescripcionCorta,
                PrecioVenta = art.PrecioVenta,
                PrecioCompra = art.PrecioCompra,
                PrecioFinal = (double)art.PrecioVenta,
                Descuento = 0,
                DescuentoPorc = 0,
                Utilidad = (double)(art.PrecioVenta - art.PrecioCompra)
            });
        }

        public class DescuentoRequest
        {
            public int IdAlmacen { get; set; }
            public string IdArticulo { get; set; } = string.Empty;
            public double Descuento { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> GuardarDescuento([FromBody] DescuentoRequest dto)
        {
            if (dto == null)
                return BadRequest();
            if (!int.TryParse(dto.IdArticulo, out int idArt))
                return BadRequest("IdArticulo inválido");

            var registro = await _context.Descuentos
                .FirstOrDefaultAsync(d => d.IdAlmacen == dto.IdAlmacen && d.IdArticulo == idArt);

            if (registro == null)
            {
                registro = new Descuento
                {
                    IdAlmacen = dto.IdAlmacen,
                    IdArticulo = idArt,
                    Descuento1 = dto.Descuento
                };
                _context.Descuentos.Add(registro);
            }
            else
            {
                registro.Descuento1 = dto.Descuento;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}