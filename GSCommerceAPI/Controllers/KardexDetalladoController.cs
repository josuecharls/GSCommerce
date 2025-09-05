using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Models;
using GSCommerceAPI.Data;
using System.Security.Claims;
using System.Linq;


namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            [FromQuery] DateTime? hasta = null,
            [FromQuery] string? familia = null,
            [FromQuery] string? linea = null)
        {
            var query = _context.VKardex3s.AsQueryable();

            if (!string.IsNullOrWhiteSpace(articulo))
            {
                query = query.Where(k =>
                    k.Codigo.Contains(articulo) || k.Articulo.Contains(articulo));
            }

            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            {
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (int.TryParse(userIdClaim, out var userId))
                {
                    var userAlmacen = await _context.Usuarios
                        .Where(u => u.IdUsuario == userId)
                        .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
                        .FirstOrDefaultAsync();

                    if (!userAlmacen.HasValue)
                        return Ok(new List<VKardex3>());

                    query = query.Where(k => k.IdAlmacen == userAlmacen.Value);
                    idAlmacen = userAlmacen.Value;
                }
                else
                {
                    return Ok(new List<VKardex3>());
                }
            }

            else if (idAlmacen.HasValue)
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

            if (!string.IsNullOrWhiteSpace(familia))
            {
                query = query.Where(k => k.Familia.Contains(familia));
            }

            if (!string.IsNullOrWhiteSpace(linea))
            {
                query = query.Where(k => k.Linea.Contains(linea));
            }

            var resultado = await query
                .OrderBy(k => k.Codigo)
                .ThenBy(k => k.Fecha)
                .ToListAsync();

            var calculado = resultado
                .GroupBy(k => new { k.IdAlmacen, k.Codigo })
                .SelectMany(g =>
                {
                    var ordenados = g
                        .OrderBy(k => k.Fecha)
                        .ThenBy(k => k.IdKardex)
                        .ToList();
                    int saldo = 0;
                    bool inicializado = false;

                    foreach (var item in ordenados)
                    {
                        if (!inicializado)
                        {
                            saldo = item.SaldoInicial;
                            inicializado = true;
                        }
                        else if (item.SaldoInicial != 0)
                        {
                            saldo = item.SaldoInicial;
                        }
                        else
                        {
                            item.SaldoInicial = saldo;
                        }

                        saldo += item.Entrada;
                        saldo -= item.Salida;
                        item.SaldoFinal = saldo;
                    }

                    return ordenados;
                })
                .ToList();

            return Ok(calculado
                .OrderBy(k => k.Codigo)
                .ThenBy(k => k.Fecha)
                .ThenBy(k => k.IdKardex));
        }
    }
}
