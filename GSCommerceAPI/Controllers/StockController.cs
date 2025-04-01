using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public StockController(SyscharlesContext context)
        {
            _context = context;
        }

        // ✅ GET: api/stock
        [HttpGet]
        public async Task<IActionResult> GetStock(
            [FromQuery] int? idAlmacen = null,
            [FromQuery] bool incluirStockCero = false)
        {
            var query = _context.VStockXalmacen1s.AsQueryable();

            if (idAlmacen.HasValue && idAlmacen > 0)
                query = query.Where(s => s.IdAlmacen == idAlmacen.Value);

            if (!incluirStockCero)
                query = query.Where(s => s.Stock > 0);

            var resultado = await query
                .OrderBy(s => s.Descripcion)
                .ToListAsync();

            return Ok(resultado);
        }
    }
}
