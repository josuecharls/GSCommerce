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
                .OrderBy(k => k.Fecha)
                .ThenBy(k => k.Codigo)
                .ToListAsync();

            var ventasQuery = _context.VDetallesVentas.AsQueryable();

            if (!string.IsNullOrWhiteSpace(articulo))
                ventasQuery = ventasQuery.Where(v => v.IdArticulo.Contains(articulo) || v.Descripcion.Contains(articulo));

            if (idAlmacen.HasValue)
                ventasQuery = ventasQuery.Where(v => v.IdAlmacen == idAlmacen.Value);

            if (desde.HasValue)
                ventasQuery = ventasQuery.Where(v => v.Fecha >= desde.Value);

            if (hasta.HasValue)
                ventasQuery = ventasQuery.Where(v => v.Fecha <= hasta.Value);

            var ventasComprobantes = await ventasQuery
                .Join(_context.ComprobanteDeVentaCabeceras,
                    v => v.IdComprobante,
                    c => c.IdComprobante,
                    (v, c) => new { v, c })
                .Join(_context.Articulos,
                    vc => vc.v.IdArticulo,
                    a => a.IdArticulo,
                    (vc, a) => new { vc.v, vc.c, a })
                .Where(va => string.IsNullOrWhiteSpace(familia) || va.a.Familia.Contains(familia))
                .Where(va => string.IsNullOrWhiteSpace(linea) || va.a.Linea.Contains(linea))
                .Join(_context.TipoDocumentoVenta,
                    va => va.c.IdTipoDocumento,
                    td => td.IdTipoDocumentoVenta,
                    (va, td) => new VKardex3
                    {
                        IdKardex = 0,
                        IdAlmacen = va.v.IdAlmacen,
                        Almacen = va.v.Almacen,
                        Familia = va.a.Familia,
                        Linea = va.a.Linea,
                        Codigo = va.v.IdArticulo,
                        Articulo = va.v.Descripcion,
                        PrecioCompra = va.v.PrecioCompra,
                        PrecioVenta = va.v.Precio,
                        Fecha = DateOnly.FromDateTime(va.v.Fecha),
                        Operacion = $"{td.Descripcion} {va.c.Serie}-{va.c.Numero}",
                        SaldoInicial = 0,
                        ValorizadoInicial = 0,
                        Entrada = 0,
                        ValorizadoEntrada = 0,
                        Salida = va.v.Cantidad ?? 0,
                        ValorizadoSalida = va.v.Costo,
                        SaldoFinal = 0,
                        ValorizadoFinal = 0,
                        ValorizadoFinalPc = 0,
                        ValorizadoFinalPv = 0
                    })
                .ToListAsync();

            var notasCreditoQuery = _context.NotaDeCreditoDetalles
                .Join(_context.NotaDeCreditoCabeceras,
                    d => d.IdNc,
                    nc => nc.IdNc,
                    (d, nc) => new { d, nc })
                .Join(_context.Articulos,
                    dn => dn.d.IdArticulo,
                    a => a.IdArticulo,
                    (dn, a) => new { dn.d, dn.nc, art = a })
                .Join(_context.Almacens,
                    dna => dna.nc.IdAlmacen,
                    al => al.IdAlmacen,
                    (dna, al) => new { dna.d, dna.nc, dna.art, al });

            if (!string.IsNullOrWhiteSpace(articulo))
                notasCreditoQuery = notasCreditoQuery.Where(n =>
                    (n.d.IdArticulo != null && n.d.IdArticulo.Contains(articulo)) ||
                    n.d.Descripcion.Contains(articulo));

            if (idAlmacen.HasValue)
                notasCreditoQuery = notasCreditoQuery.Where(n => n.nc.IdAlmacen == idAlmacen.Value);

            if (desde.HasValue)
                notasCreditoQuery = notasCreditoQuery.Where(n => n.nc.Fecha >= desde.Value);

            if (hasta.HasValue)
                notasCreditoQuery = notasCreditoQuery.Where(n => n.nc.Fecha <= hasta.Value);

            if (!string.IsNullOrWhiteSpace(familia))
                notasCreditoQuery = notasCreditoQuery.Where(n => n.art.Familia.Contains(familia));

            if (!string.IsNullOrWhiteSpace(linea))
                notasCreditoQuery = notasCreditoQuery.Where(n => n.art.Linea.Contains(linea));

            var notasCredito = await notasCreditoQuery
                .Join(_context.TipoDocumentoVenta,
                    n => n.nc.IdTipoDocumento,
                    td => td.IdTipoDocumentoVenta,
                    (n, td) => new VKardex3
                    {
                        IdKardex = 0,
                        IdAlmacen = n.nc.IdAlmacen,
                        Almacen = n.al.Nombre,
                        Familia = n.art.Familia,
                        Linea = n.art.Linea,
                        Codigo = n.d.IdArticulo ?? string.Empty,
                        Articulo = n.d.Descripcion,
                        PrecioCompra = n.art.PrecioCompra,
                        PrecioVenta = n.art.PrecioVenta,
                        Fecha = DateOnly.FromDateTime(n.nc.Fecha),
                        Operacion = $"{td.Descripcion} {n.nc.Serie}-{n.nc.Numero}",
                        SaldoInicial = 0,
                        ValorizadoInicial = 0,
                        Entrada = n.d.Cantidad ?? 0,
                        ValorizadoEntrada = n.d.Total,
                        Salida = 0,
                        ValorizadoSalida = 0,
                        SaldoFinal = 0,
                        ValorizadoFinal = 0,
                        ValorizadoFinalPc = 0,
                        ValorizadoFinalPv = 0
                    })
                .ToListAsync();

            resultado.AddRange(ventasComprobantes);
            resultado.AddRange(notasCredito);

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

                        //saldo += item.Entrada;
                        saldo -= item.Salida;
                        item.SaldoFinal = saldo;
                    }

                    return ordenados;
                })
                .ToList();

            return Ok(calculado
                .OrderBy(k => k.Fecha)
                .ThenBy(k => k.IdKardex));
        }
    }
}
