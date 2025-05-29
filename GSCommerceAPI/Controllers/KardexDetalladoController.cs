using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Models;
using GSCommerceAPI.Data;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KardexDetalladoController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public KardexDetalladoController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/KardexDetallado
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VKardex3>>> GetKardexDetallado(
            [FromQuery] string? articulo = null,
            [FromQuery] int? idAlmacen = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var query = _context.VKardex3s.AsQueryable();

            if (!string.IsNullOrWhiteSpace(articulo))
            {
                query = query.Where(k =>
                    k.Codigo.Contains(articulo) || k.Articulo.Contains(articulo));
            }

            if (idAlmacen.HasValue)
            {
                query = query.Where(k => k.IdAlmacen == idAlmacen.Value);
            }

            if (desde.HasValue)
            {
                query = query.Where(k => k.Fecha >= DateOnly.FromDateTime(desde.Value));
            }

            if (hasta.HasValue)
            {
                query = query.Where(k => k.Fecha <= DateOnly.FromDateTime(hasta.Value));
            }

            return await query.OrderByDescending(k => k.Fecha).ThenBy(k => k.Codigo).ToListAsync();
        }
    }
}
