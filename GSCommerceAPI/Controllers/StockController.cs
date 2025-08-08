using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var cargo = User.FindFirst("Cargo")?.Value;
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (cargo == "CAJERO" && int.TryParse(userIdClaim, out var userId))
            {
                var idAlmacenUsuario = await _context.Usuarios
                    .Include(u => u.IdPersonalNavigation)
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => u.IdPersonalNavigation.IdAlmacen)
                    .FirstOrDefaultAsync();
                idAlmacen = idAlmacenUsuario;
            }

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

            var totalCount = await query.CountAsync();

            var items = await query
        .OrderBy(s => s.Descripcion)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(s => new StockDTO
        {
            IdAlmacen = s.IdAlmacen,
            Almacen = s.Almacen,
            Familia = s.Familia,
            Linea = s.Linea,
            IdArticulo = s.IdArticulo,
            Descripcion = s.Descripcion,
            Stock = s.Stock,
            PrecioCompra = cargo == "CAJERO" ? 0 : s.PrecioCompra,
            ValorCompra = cargo == "CAJERO" ? (decimal?)null : s.ValorCompra,
            PrecioVenta = cargo == "CAJERO" ? (double?)null : s.PrecioVenta,
            ValorVenta = cargo == "CAJERO" ? (double?)null : s.ValorVenta,
            StockMinimo = s.StockMinimo,
            // Si no se especifica un stock mínimo, usar 5 como límite
            EstaBajoMinimo = s.StockMinimo.HasValue
                ? s.Stock < s.StockMinimo.Value
                : s.Stock < 5
        })
        .ToListAsync();

                var resultado = new PagedResult<StockDTO>
                {
                    Items = items,
                    TotalCount = totalCount
                };

                return Ok(resultado);
        }
    }
}
