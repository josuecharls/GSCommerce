using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KardexController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public KardexController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VKardexGeneral>>> GetKardex(
            [FromQuery] string? articulo,
            [FromQuery] int? idAlmacen,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] string? familia,
            [FromQuery] string? linea)
        {
            var query = _context.VKardexGenerals.AsQueryable();

            if (!string.IsNullOrEmpty(articulo))
                query = query.Where(k => k.Codigo.Contains(articulo) || k.Articulo.Contains(articulo));

            if (idAlmacen.HasValue && idAlmacen > 0)
                query = query.Where(k => k.IdAlmacen == idAlmacen.Value);

            if (desde.HasValue)
                query = query.Where(k => k.Fecha >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(k => k.Fecha <= hasta.Value);

            if (!string.IsNullOrWhiteSpace(familia))
                query = query.Where(k => k.Familia.Contains(familia));

            if (!string.IsNullOrWhiteSpace(linea))
                query = query.Where(k => k.Linea.Contains(linea));

            var resultado = await query
                .OrderByDescending(k => k.Fecha)
                .ToListAsync();

            return Ok(resultado);
        }
    }
}
