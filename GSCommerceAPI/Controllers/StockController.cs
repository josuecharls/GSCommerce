using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private const int LowStockThreshold = 10;

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
            var esCajero = EsCajero(cargo);
            var idAlmacenFiltrado = await ResolverIdAlmacenAsync(cargo, userIdClaim, idAlmacen);

            if (esCajero && !idAlmacenFiltrado.HasValue)
            {
                return Ok(new PagedResult<StockDTO>());
            }

            var pageNumber = Math.Max(1, page);
            var size = pageSize > 0 ? pageSize : 50;
            var skip = (pageNumber - 1) * size;
            var terminoBusqueda = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var query = _context.VStockXalmacen1s.AsNoTracking().AsQueryable();

            if (idAlmacenFiltrado.HasValue && idAlmacenFiltrado.Value > 0)
            {
                query = query.Where(s => s.IdAlmacen == idAlmacenFiltrado.Value);
            }

            if (!incluirStockCero)
            {
                query = query.Where(s => s.Stock > 0);
            }

            if (!string.IsNullOrEmpty(terminoBusqueda))
            {
                if (filtroBusqueda == 1)
                {
                    query = query.Where(s => s.IdArticulo.Contains(terminoBusqueda));
                }
                else if (filtroBusqueda == 2)
                {
                    query = query.Where(s => s.Descripcion.Contains(terminoBusqueda));
                }
            }

            var totalCount = await query.CountAsync();

            var registros = await query
                .OrderBy(s => s.Descripcion)
                .Skip(skip)
                .Take(size)
                .Select(s => new StockProjection
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
                    StockMinimo = s.StockMinimo
                })
                .ToListAsync();

            var items = registros
                .Select(s => MapToStockDto(s, esCajero, LowStockThreshold, fillMissingMinimo: false))
                .ToList();

            var resultado = new PagedResult<StockDTO>
            {
                Items = items,
                TotalCount = totalCount
            };

            return Ok(resultado);
        }

        // ✅ GET: api/stock/low-stock
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock(
            [FromQuery] int? idAlmacen = null,
            [FromQuery] int threshold = LowStockThreshold)
        {
            var cargo = User.FindFirst("Cargo")?.Value;
            var esCajero = EsCajero(cargo);
            var userIdClaim = User.FindFirst("userId")?.Value;
            var idAlmacenFiltrado = await ResolverIdAlmacenAsync(cargo, userIdClaim, idAlmacen);

            if (esCajero && !idAlmacenFiltrado.HasValue)
            {
                return Ok(new List<StockDTO>());
            }

            var umbral = threshold > 0 ? threshold : LowStockThreshold;

            var query = _context.VStockXalmacen1s.AsNoTracking().AsQueryable();

            if (idAlmacenFiltrado.HasValue && idAlmacenFiltrado.Value > 0)
            {
                query = query.Where(s => s.IdAlmacen == idAlmacenFiltrado.Value);
            }

            query = query.Where(s =>
                s.Stock > 0 &&
                (
                    (s.StockMinimo.HasValue && s.StockMinimo.Value > 0 && s.Stock <= s.StockMinimo.Value)
                    || ((!s.StockMinimo.HasValue || s.StockMinimo.Value <= 0) && s.Stock <= umbral)
                ));

            var registros = await query
                .OrderBy(s => s.Stock)
                .ThenBy(s => s.Descripcion)
                .Select(s => new StockProjection
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
                    StockMinimo = s.StockMinimo
                })
                .ToListAsync();

            var articulos = registros
                .Select(s => MapToStockDto(s, esCajero, umbral, fillMissingMinimo: true))
                .ToList();

            return Ok(articulos);
        }

        // ✅ GET: api/stock/reposition-alert
        [HttpGet("reposition-alert")]
        public async Task<IActionResult> GetRepositionAlert(
            [FromQuery] int? idAlmacen = null,
            [FromQuery] int threshold = LowStockThreshold)
        {
            var cargo = User.FindFirst("Cargo")?.Value;
            var esAdministrador = string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase);
            
            if (!esAdministrador)
            {
                return Ok(new List<RepositionAlertDTO>());
            }

            // Buscar almacén PRINCIPAL
            var almacenPrincipal = await _context.Almacens
                .FirstOrDefaultAsync(a => a.Nombre.ToUpper().Contains("PRINCIPAL"));

            if (almacenPrincipal == null)
            {
                return Ok(new List<RepositionAlertDTO>());
            }

            var umbral = threshold > 0 ? threshold : LowStockThreshold;

            // Obtener artículos con stock bajo en almacén PRINCIPAL
            var articulosStockBajo = await _context.VStockXalmacen1s
                .Where(s => s.IdAlmacen == almacenPrincipal.IdAlmacen && 
                           s.Stock > 0 && 
                           (s.Stock <= umbral || 
                            (s.StockMinimo.HasValue && s.StockMinimo.Value > 0 && s.Stock <= s.StockMinimo.Value)))
                .Select(s => new { s.IdArticulo, s.Descripcion, s.Stock })
                .ToListAsync();

            var alertas = new List<RepositionAlertDTO>();

            foreach (var articulo in articulosStockBajo)
            {
                // Buscar stock disponible en otros almacenes
                var stockEnOtrosAlmacenes = await _context.VStockXalmacen1s
                    .Where(s => s.IdArticulo == articulo.IdArticulo && 
                               s.IdAlmacen != almacenPrincipal.IdAlmacen && 
                               s.Stock > 0)
                    .Select(s => new { s.IdAlmacen, s.Almacen, s.Stock })
                    .ToListAsync();

                if (stockEnOtrosAlmacenes.Any())
                {
                    alertas.Add(new RepositionAlertDTO
                    {
                        IdArticulo = articulo.IdArticulo,
                        Descripcion = articulo.Descripcion,
                        StockPrincipal = articulo.Stock,
                        AlmacenesConStock = stockEnOtrosAlmacenes.Select(a => 
                            new AlmacenStockDTO 
                            { 
                                IdAlmacen = a.IdAlmacen, 
                                Nombre = a.Almacen, 
                                Stock = a.Stock 
                            }).ToList()
                    });
                }
            }

            return Ok(alertas);
        }

        private async Task<int?> ResolverIdAlmacenAsync(string? cargo, string? userIdClaim, int? idAlmacenParametro)
        {
            if (EsCajero(cargo))
            {
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var almacenUsuario = await _context.Usuarios
                        .Include(u => u.IdPersonalNavigation)
                        .Where(u => u.IdUsuario == userId)
                        .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
                        .FirstOrDefaultAsync();

                    if (almacenUsuario.HasValue && almacenUsuario.Value > 0)
                    {
                        return almacenUsuario;
                    }
                }

                if (idAlmacenParametro.HasValue && idAlmacenParametro.Value > 0)
                {
                    return idAlmacenParametro.Value;
                }

                return null;
            }

            if (idAlmacenParametro.HasValue && idAlmacenParametro.Value > 0)
            {
                return idAlmacenParametro.Value;
            }

            return null;
        }

        private static bool EsCajero(string? cargo)
        {
            return string.Equals(cargo, "CAJERO", StringComparison.OrdinalIgnoreCase);
        }

        private StockDTO MapToStockDto(StockProjection stock, bool esCajero, int threshold, bool fillMissingMinimo)
        {
            var stockMinimo = stock.StockMinimo.HasValue && stock.StockMinimo.Value > 0
                ? stock.StockMinimo.Value
                : (int?)null;

            var referenciaMinima = stockMinimo ?? threshold;
            var estaBajoMinimo = stock.Stock <= referenciaMinima;

            return new StockDTO
            {
                IdAlmacen = stock.IdAlmacen,
                Almacen = stock.Almacen,
                Familia = stock.Familia,
                Linea = stock.Linea,
                IdArticulo = stock.IdArticulo,
                Descripcion = stock.Descripcion,
                Stock = stock.Stock,
                PrecioCompra = esCajero ? 0m : stock.PrecioCompra,
                ValorCompra = esCajero ? (decimal?)null : stock.ValorCompra,
                PrecioVenta = stock.PrecioVenta,
                ValorVenta = stock.ValorVenta,
                StockMinimo = fillMissingMinimo ? referenciaMinima : stockMinimo,
                EstaBajoMinimo = estaBajoMinimo
            };
        }

        private sealed class StockProjection
        {
            public int IdAlmacen { get; init; }
            public string Almacen { get; init; } = string.Empty;
            public string Familia { get; init; } = string.Empty;
            public string Linea { get; init; } = string.Empty;
            public string IdArticulo { get; init; } = string.Empty;
            public string Descripcion { get; init; } = string.Empty;
            public int Stock { get; init; }
            public decimal PrecioCompra { get; init; }
            public decimal? ValorCompra { get; init; }
            public double? PrecioVenta { get; init; }
            public double? ValorVenta { get; init; }
            public int? StockMinimo { get; init; }
        }
    }
}