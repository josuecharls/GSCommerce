using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;

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
    [FromQuery] bool incluirStockCero = false,
    [FromQuery] int filtroBusqueda = 1, // 1: Código, 2: Descripción
    [FromQuery] string? search = null)
        {
            var query = _context.VStockXalmacen1s.AsQueryable();

            if (idAlmacen.HasValue && idAlmacen > 0)
                query = query.Where(s => s.IdAlmacen == idAlmacen.Value);

            if (!incluirStockCero)
                query = query.Where(s => s.Stock > 0);

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (filtroBusqueda == 1)
                    query = query.Where(s => s.IdArticulo.Contains(search));
                else if (filtroBusqueda == 2)
                    query = query.Where(s => s.Descripcion.Contains(search));
            }

            var resultado = await query
    .OrderBy(s => s.Descripcion)
    .Select(s => new StockDTO
    {
        IdAlmacen = s.IdAlmacen,
        Almacen = s.Almacen,
        Familia = s.Familia,
        Linea = s.Linea,
        IdArticulo = s.IdArticulo,
        Descripcion = s.Descripcion,
        Stock = s.Stock,
        PrecioCompra = s.PrecioCompra,
        ValorCompra = s.ValorCompra,
        PrecioVenta = s.PrecioVenta,
        ValorVenta = s.ValorVenta,
        StockMinimo = s.StockMinimo,
        // Si no se especifica un stock mínimo, usar 5 como límite
        EstaBajoMinimo = s.StockMinimo.HasValue
            ? s.Stock < s.StockMinimo.Value
            : s.Stock < 5
    })
    .ToListAsync();
            return Ok(resultado);
        }
    }
}
