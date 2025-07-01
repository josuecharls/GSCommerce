using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/toma-inventarios")]
    public class TomaInventariosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public TomaInventariosController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTomas([FromQuery] int? anio = null, [FromQuery] int? idAlmacen = null)
        {
            var query = _context.TomaInventarios
                .Include(t => t.IdAlmacenNavigation)
                .AsQueryable();

            if (anio.HasValue)
                query = query.Where(t => t.Fecha.Year == anio.Value);

            if (idAlmacen.HasValue)
                query = query.Where(t => t.IdAlmacen == idAlmacen.Value);

            var lista = await query
                .OrderByDescending(t => t.Fecha)
                .Select(t => new
                {
                    t.IdTomaInventario,
                    t.IdAlmacen,
                    t.IdAlmacenNavigation.Nombre,
                    t.Fecha,
                    t.EstadoToma,
                    t.Estado,
                    t.Intervienen
                })
                .ToListAsync();

            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetToma(int id)
        {
            var toma = await _context.TomaInventarios
                .Include(t => t.TomaInventarioDetalles)
                .FirstOrDefaultAsync(t => t.IdTomaInventario == id);

            if (toma == null)
                return NotFound();

            return Ok(toma);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TomaInventario dto)
        {
            dto.Fecha = DateTime.Now;
            dto.EstadoToma = "Iniciado";
            dto.Estado = true;
            _context.TomaInventarios.Add(dto);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetToma), new { id = dto.IdTomaInventario }, dto);
        }

        [HttpGet("{id}/detalles")]
        public async Task<IActionResult> GetDetalles(int id)
        {
            var detalles = await _context.TomaInventarioDetalles
                .Where(d => d.IdTomaInventario == id)
                .Include(d => d.IdArticuloNavigation)
                .Select(d => new
                {
                    d.IdTomaInventarioDetalle,
                    d.IdTomaInventario,
                    d.IdArticulo,
                    d.Cantidad,
                    d.Estado,
                    d.Sobrante,
                    d.Faltante,
                    Nombre = d.IdArticuloNavigation.Descripcion
                })
                .ToListAsync();
            return Ok(detalles);
        }

        [HttpPost("{id}/detalles")]
        public async Task<IActionResult> AgregarDetalle(int id, TomaInventarioDetalle detalle)
        {
            var existe = await _context.TomaInventarios.AnyAsync(t => t.IdTomaInventario == id);
            if (!existe)
                return NotFound();

            detalle.IdTomaInventario = id;
            detalle.Estado = true;
            _context.TomaInventarioDetalles.Add(detalle);
            await _context.SaveChangesAsync();
            return Ok(detalle);
        }

        [HttpPut("{id}/terminar")]
        public async Task<IActionResult> Terminar(int id)
        {
            var toma = await _context.TomaInventarios.FindAsync(id);
            if (toma == null)
                return NotFound();

            toma.EstadoToma = "Terminado";
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}/aplicar-diferencias")]
        public async Task<IActionResult> AplicarDiferencias(int id)
        {
            var toma = await _context.TomaInventarios
                .Include(t => t.TomaInventarioDetalles)
                .FirstOrDefaultAsync(t => t.IdTomaInventario == id);

            if (toma == null)
                return NotFound();

            if (toma.EstadoToma != "Terminado")
                return BadRequest("La toma de inventario no está terminada.");

            foreach (var d in toma.TomaInventarioDetalles)
            {
                if (!d.Estado) continue;

                var stock = await _context.StockAlmacens
                    .FirstOrDefaultAsync(s => s.IdAlmacen == toma.IdAlmacen && s.IdArticulo == d.IdArticulo);

                if (stock == null)
                {
                    stock = new StockAlmacen
                    {
                        IdAlmacen = toma.IdAlmacen,
                        IdArticulo = d.IdArticulo,
                        Stock = 0,
                        StockMinimo = 0
                    };
                    _context.StockAlmacens.Add(stock);
                }

                var actual = stock.Stock;
                var diff = d.Cantidad - actual;
                d.Sobrante = diff > 0 ? diff : 0;
                d.Faltante = diff < 0 ? -diff : 0;

                if (diff != 0)
                {
                    var precio = await _context.Articulos
                        .Where(a => a.IdArticulo == d.IdArticulo)
                        .Select(a => a.PrecioCompra)
                        .FirstOrDefaultAsync();

                    var kardex = new Kardex
                    {
                        IdAlmacen = toma.IdAlmacen,
                        IdArticulo = d.IdArticulo,
                        TipoMovimiento = diff > 0 ? "I" : "E",
                        Fecha = DateTime.Now,
                        SaldoInicial = actual,
                        Cantidad = Math.Abs(diff),
                        SaldoFinal = d.Cantidad,
                        Valor = precio,
                        Origen = (diff > 0 ? "Sobrante" : "Faltante") +
                                 $" por Toma de Inventario Nro: {id}"
                    };

                    _context.Kardices.Add(kardex);
                }

                stock.Stock = d.Cantidad;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }


        [HttpPut("{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            var toma = await _context.TomaInventarios.FindAsync(id);
            if (toma == null)
                return NotFound();

            toma.EstadoToma = "Anulado";
            toma.Estado = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}