using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
                        return Ok(new List<VKardexGeneral>());

                    query = query.Where(k => k.IdAlmacen == userAlmacen.Value);
                    idAlmacen = userAlmacen.Value;
                }
                else
                {
                    return Ok(new List<VKardexGeneral>());
                }
            }
            else if (idAlmacen.HasValue && idAlmacen > 0)
            {
                query = query.Where(k => k.IdAlmacen == idAlmacen.Value);
            }

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

            var calculado = resultado
                .GroupBy(k => new { k.IdAlmacen, k.Codigo })
                .SelectMany(g =>
                {
                    var ordenados = g.OrderBy(k => k.Fecha).ToList();
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
                        saldo -= item.Salida ?? 0;
                        item.SaldoFinal = saldo;
                    }

                    return ordenados;
                })
                .ToList();

            var agrupado = calculado
                .GroupBy(k => new
                {
                    k.IdAlmacen,
                    k.Almacen,
                    k.Familia,
                    k.Linea,
                    k.Codigo,
                    k.Articulo,
                    k.PrecioCompra,
                    k.PrecioVenta
                })
                .Select(g =>
                {
                    var ordenados = g.OrderBy(k => k.Fecha).ToList();
                    var primero = ordenados.First();
                    var ultimo = ordenados.Last();

                    return new VKardexGeneral
                    {
                        IdKardex = 0,
                        IdAlmacen = g.Key.IdAlmacen,
                        Almacen = g.Key.Almacen,
                        Familia = g.Key.Familia,
                        Linea = g.Key.Linea,
                        Codigo = g.Key.Codigo,
                        Articulo = g.Key.Articulo,
                        PrecioCompra = g.Key.PrecioCompra,
                        PrecioVenta = g.Key.PrecioVenta,
                        Fecha = null,
                        Operacion = string.Empty,
                        SaldoInicial = primero.SaldoInicial,
                        ValorizadoInicial = primero.ValorizadoInicial,
                        Entrada = ordenados.Sum(x => x.Entrada),
                        ValorizadoEntrada = ordenados.Sum(x => x.ValorizadoEntrada ?? 0),
                        Salida = ordenados.Sum(x => x.Salida ?? 0),
                        ValorizadoSalida = ordenados.Sum(x => x.ValorizadoSalida ?? 0),
                        SaldoFinal = primero.SaldoInicial + ordenados.Sum(x => x.Entrada) - ordenados.Sum(x => x.Salida ?? 0),
                        ValorizadoFinal = ultimo.ValorizadoFinal,
                        ValorizadoFinalPc = ultimo.ValorizadoFinalPc,
                        ValorizadoFinalPv = ultimo.ValorizadoFinalPv
                    };
                })
                .OrderBy(k => k.Codigo)
                .ToList();

            return Ok(agrupado);
        }
    }
}
