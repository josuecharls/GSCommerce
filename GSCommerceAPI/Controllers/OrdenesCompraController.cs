using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/ordenes-compra")]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public OrdenesCompraController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrdenCompra(int id)
        {
            var orden = await _context.OrdenDeCompraCabeceras
                .Include(o => o.OrdenDeCompraDetalles)
                .FirstOrDefaultAsync(o => o.IdOc == id);

            if (orden == null)
                return NotFound();

            return Ok(new OrdenCompraDTO
            {
                IdOc = orden.IdOc,
                IdProveedor = orden.IdProveedor,
                NumeroOc = orden.NumeroOc,
                FechaOc = orden.FechaOc,
                RucProveedor = orden.Rucproveedor,
                NombreProveedor = orden.NombreProveedor,
                DireccionProveedor = orden.DireccionProveedor,
                Moneda = orden.Moneda,
                TipoCambio = orden.TipoCambio,
                FormaPago = orden.FormaPago,
                SinIgv = orden.SinIgv,
                FechaEntrega = orden.FechaEntrega,
                Atencion = orden.Atencion,
                Glosa = orden.Glosa,
                ImporteSubTotal = orden.ImporteSubTotal,
                ImporteIgv = orden.ImporteIgv,
                ImporteTotal = orden.ImporteTotal,
                EstadoEmision = orden.EstadoEmision,
                EstadoAtencion = orden.EstadoAtencion,
                Detalles = orden.OrdenDeCompraDetalles
                    .OrderBy(d => d.Item)
                    .Select(d => new OrdenCompraDetalleDTO
                    {
                        IdOc = d.IdOc,
                        Item = d.Item,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        UnidadMedida = d.UnidadMedida,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario,
                        Total = d.Total
                    }).ToList()
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> List(DateTime desde, DateTime hasta, int? idProveedor = null)
        {
            var inicio = desde.Date;
            var finExclusivo = hasta.Date.AddDays(1);

            var query = _context.VListadoOc1s
                .Where(o => o.FechaOc >= inicio && o.FechaOc < finExclusivo);
            if (idProveedor.HasValue && idProveedor.Value > 0)
                query = query.Where(o => o.IdProveedor == idProveedor.Value);

            var lista = await query
                .OrderByDescending(o => o.FechaOc)
                .Select(o => new OrdenCompraConsultaDTO
                {
                    IdOc = o.IdOc,
                    IdProveedor = o.IdProveedor,
                    Nombre = o.Nombre,
                    NumeroOc = o.NumeroOc,
                    FechaOc = o.FechaOc,
                    FechaEntrega = o.FechaEntrega,
                    ImporteSubTotal = o.ImporteSubTotal,
                    ImporteIgv = o.ImporteIgv,
                    ImporteTotal = o.ImporteTotal,
                    Estado = o.Estado,
                    FechaAtencionTotal = o.FechaAtencionTotal,
                    FechaAnulado = o.FechaAnulado,
                    Glosa = o.Glosa
                })
                .ToListAsync();

            var idsOc = lista.Select(l => l.IdOc).ToList();
            if (idsOc.Any())
            {
                var estados = await _context.OrdenDeCompraCabeceras
                    .Where(o => idsOc.Contains(o.IdOc))
                    .Select(o => new { o.IdOc, o.EstadoAtencion })
                    .ToDictionaryAsync(o => o.IdOc, o => o.EstadoAtencion);

                foreach (var item in lista)
                {
                    if (estados.TryGetValue(item.IdOc, out var estadoAtencion) &&
                        !string.IsNullOrEmpty(estadoAtencion))
                    {
                        var estadoNormalizado = estadoAtencion.Trim();
                        if (estadoNormalizado.Equals("IN", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Estado = "INGRESADO";
                        }
                        else if (estadoNormalizado.Equals("AN", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Estado = "ANULADO";
                        }
                    }
                }
            }

            var idsPagos = lista
                .Where(x => x.NumeroOc.StartsWith("PP-", StringComparison.OrdinalIgnoreCase) && int.TryParse(x.NumeroOc[3..], out _))
                .Select(x => int.Parse(x.NumeroOc[3..]))
                .ToList();

            if (idsPagos.Any())
            {
                var almacenes = await _context.IngresosEgresosCabeceras
                    .Include(i => i.IdAlmacenNavigation)
                    .Where(i => idsPagos.Contains(i.IdIngresoEgreso))
                    .ToDictionaryAsync(i => i.IdIngresoEgreso, i => i.IdAlmacenNavigation.Nombre);

                foreach (var item in lista)
                {
                    if (item.NumeroOc.StartsWith("PP-", StringComparison.OrdinalIgnoreCase) && int.TryParse(item.NumeroOc[3..], out var idIe) && almacenes.TryGetValue(idIe, out var nomAlmacen))
                    {
                        item.Almacen = nomAlmacen;
                    }
                }
            }

            return Ok(lista);
        }


        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] OrdenCompraDTO dto)
        {
            if (dto == null || dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Datos incompletos.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var cabecera = new OrdenDeCompraCabecera
                {
                    IdProveedor = dto.IdProveedor,
                    NumeroOc = dto.NumeroOc,
                    FechaOc = dto.FechaOc,
                    Rucproveedor = dto.RucProveedor,
                    NombreProveedor = dto.NombreProveedor,
                    DireccionProveedor = dto.DireccionProveedor,
                    Moneda = dto.Moneda,
                    TipoCambio = dto.TipoCambio,
                    FormaPago = dto.FormaPago,
                    SinIgv = dto.SinIgv,
                    FechaEntrega = dto.FechaEntrega,
                    Atencion = dto.Atencion,
                    Glosa = dto.Glosa,
                    ImporteSubTotal = dto.ImporteSubTotal,
                    ImporteIgv = dto.ImporteIgv,
                    ImporteTotal = dto.ImporteTotal,
                    EstadoEmision = false,
                    EstadoAtencion = "PE",
                    FechaRegistra = DateTime.Now
                };

                _context.OrdenDeCompraCabeceras.Add(cabecera);
                await _context.SaveChangesAsync();

                int item = 1;
                foreach (var d in dto.Detalles)
                {
                    var detalle = new OrdenDeCompraDetalle
                    {
                        IdOc = cabecera.IdOc,
                        Item = item++,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        UnidadMedida = d.UnidadMedida,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario,
                        Total = d.Total
                    };
                    _context.OrdenDeCompraDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                dto.IdOc = cabecera.IdOc;
                return Ok(dto);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error al registrar OC: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] OrdenCompraDTO dto)
        {
            if (dto == null || dto.Detalles == null || !dto.Detalles.Any())
                return BadRequest("Datos incompletos.");

            var orden = await _context.OrdenDeCompraCabeceras
                .Include(o => o.OrdenDeCompraDetalles)
                .FirstOrDefaultAsync(o => o.IdOc == id);

            if (orden == null)
                return NotFound();

            if (!string.Equals(orden.EstadoAtencion, "PE", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Solo las órdenes pendientes pueden editarse.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                orden.IdProveedor = dto.IdProveedor;
                orden.NumeroOc = dto.NumeroOc;
                orden.FechaOc = dto.FechaOc;
                orden.Rucproveedor = dto.RucProveedor;
                orden.NombreProveedor = dto.NombreProveedor;
                orden.DireccionProveedor = dto.DireccionProveedor;
                orden.Moneda = dto.Moneda;
                orden.TipoCambio = dto.TipoCambio;
                orden.FormaPago = dto.FormaPago;
                orden.SinIgv = dto.SinIgv;
                orden.FechaEntrega = dto.FechaEntrega;
                orden.Atencion = dto.Atencion;
                orden.Glosa = dto.Glosa;
                orden.ImporteSubTotal = dto.ImporteSubTotal;
                orden.ImporteIgv = dto.ImporteIgv;
                orden.ImporteTotal = dto.ImporteTotal;

                _context.OrdenDeCompraDetalles.RemoveRange(orden.OrdenDeCompraDetalles);

                int item = 1;
                foreach (var d in dto.Detalles)
                {
                    var detalle = new OrdenDeCompraDetalle
                    {
                        IdOc = orden.IdOc,
                        Item = item++,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        UnidadMedida = d.UnidadMedida,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario,
                        Total = d.Total
                    };
                    _context.OrdenDeCompraDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error al actualizar OC: {ex.Message}");
            }
        }

        [HttpPost("{id}/generar-ingreso")]
        public async Task<IActionResult> GenerarIngreso(int id, int idAlmacen)
        {
            if (idAlmacen != 1009)
                return BadRequest("El ingreso por orden de compra debe registrarse en el Almacen Principal.");

            var orden = await _context.OrdenDeCompraCabeceras
                .Include(o => o.OrdenDeCompraDetalles)
                .FirstOrDefaultAsync(o => o.IdOc == id);
            if (orden == null) return NotFound();
            if (!string.Equals(orden.EstadoAtencion, "PE", StringComparison.OrdinalIgnoreCase))
                return BadRequest("La orden ya fue ingresada o no está pendiente.");

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var mov = new MovimientosCabecera
                {
                    IdAlmacen = idAlmacen,
                    Tipo = "I",
                    Motivo = "OC",
                    Fecha = DateOnly.FromDateTime(DateTime.Now),
                    Descripcion = orden.Glosa,
                    IdProveedor = orden.IdProveedor,
                    IdOc = orden.IdOc,
                    IdUsuario = orden.IdUsuarioRegistra ?? 0,
                    FechaHoraRegistro = DateTime.Now,
                    Estado = "E"
                };
                _context.MovimientosCabeceras.Add(mov);
                await _context.SaveChangesAsync();

                int item = 1;
                foreach (var d in orden.OrdenDeCompraDetalles)
                {
                    var det = new MovimientosDetalle
                    {
                        IdMovimiento = mov.IdMovimiento,
                        Item = item++,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        Cantidad = d.Cantidad,
                        Valor = d.CostoUnitario
                    };
                    _context.MovimientosDetalles.Add(det);
                }

                await _context.SaveChangesAsync();

                // Aumentar stock y registrar en kardex por cada detalle
                foreach (var d in orden.OrdenDeCompraDetalles)
                {
                    var stock = await _context.StockAlmacens
                        .FirstOrDefaultAsync(s => s.IdAlmacen == idAlmacen && s.IdArticulo == d.IdArticulo);

                    var saldoInicial = stock?.Stock ?? 0;
                    if (stock == null)
                    {
                        stock = new StockAlmacen
                        {
                            IdAlmacen = idAlmacen,
                            IdArticulo = d.IdArticulo,
                            Stock = 0,
                            StockMinimo = 0
                        };
                        _context.StockAlmacens.Add(stock);
                    }

                    stock.Stock += d.Cantidad;
                    var saldoFinal = stock.Stock;

                    _context.Kardices.Add(new Kardex
                    {
                        IdAlmacen = idAlmacen,
                        IdArticulo = d.IdArticulo,
                        TipoMovimiento = "I",
                        Fecha = DateTime.Now,
                        SaldoInicial = saldoInicial,
                        Cantidad = d.Cantidad,
                        SaldoFinal = saldoFinal,
                        Valor = d.CostoUnitario,
                        Origen = orden.Glosa,
                        NoKardexGeneral = false
                    });
                }

                await _context.SaveChangesAsync();

                orden.EstadoAtencion = "IN";
                orden.FechaAtencionTotal = DateTime.Now;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { mov.IdMovimiento, Estado = "INGRESADO" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error al generar ingreso: {ex.Message}");
            }
        }

        [HttpPost("{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            var orden = await _context.OrdenDeCompraCabeceras
                .Include(o => o.OrdenDeCompraDetalles)
                .FirstOrDefaultAsync(o => o.IdOc == id);

            if (orden == null)
                return NotFound();

            var estadoActual = orden.EstadoAtencion?.Trim() ?? string.Empty;

            if (string.Equals(estadoActual, "AN", StringComparison.OrdinalIgnoreCase))
                return BadRequest("La orden ya fue anulada.");

            if (!string.Equals(estadoActual, "IN", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Solo se pueden anular órdenes ingresadas.");

            var movimientos = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .Where(m => m.IdOc == id)
                .OrderByDescending(m => m.FechaHoraRegistro)
                .ToListAsync();

            if (!movimientos.Any())
                return BadRequest("No se encontró un movimiento de ingreso asociado a la orden.");

            var movimientoActivo = movimientos
                .FirstOrDefault(m => !string.Equals(m.Estado?.Trim(), "A", StringComparison.OrdinalIgnoreCase));

            if (movimientoActivo == null)
                return BadRequest("El movimiento de ingreso ya se encuentra anulado.");

            using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var fecha = DateTime.Now;
                movimientoActivo.Estado = "A";

                foreach (var det in movimientoActivo.MovimientosDetalles)
                {
                    var stock = await _context.StockAlmacens
                        .FirstOrDefaultAsync(s => s.IdAlmacen == movimientoActivo.IdAlmacen && s.IdArticulo == det.IdArticulo);

                    if (stock == null || stock.Stock < det.Cantidad)
                    {
                        await tx.RollbackAsync();
                        return BadRequest($"Stock insuficiente para el artículo {det.IdArticulo} en el almacén {movimientoActivo.IdAlmacen}.");
                    }

                    var saldoInicial = stock.Stock;
                    stock.Stock -= det.Cantidad;

                    _context.Kardices.Add(new Kardex
                    {
                        IdAlmacen = movimientoActivo.IdAlmacen,
                        IdArticulo = det.IdArticulo,
                        TipoMovimiento = "E",
                        Fecha = fecha,
                        SaldoInicial = saldoInicial,
                        Cantidad = det.Cantidad,
                        SaldoFinal = stock.Stock,
                        Valor = det.Valor,
                        Origen = $"ANULACIÓN OC {orden.NumeroOc}",
                        NoKardexGeneral = false
                    });
                }

                orden.EstadoAtencion = "AN";
                orden.FechaAnulado = fecha;
                orden.FechaAtencionTotal = null;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { Estado = "ANULADO" });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, $"Error al anular la orden: {ex.Message}");
            }
        }

    }
}