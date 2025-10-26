using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using GSCommerceAPI.Helpers;
using Microsoft.AspNetCore.Authorization;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/movimientos-guias")]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO")]
    public class MovimientosGuiasController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public MovimientosGuiasController(SyscharlesContext context)
        {
            _context = context;
        }

        // ✅ GET: api/movimientosguias/list
        [HttpGet("list")]
        public async Task<IActionResult> GetMovimientosFiltrados(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string tipo = "",
            [FromQuery] string search = "",
            [FromQuery] string motivo = "",
            [FromQuery] int? idAlmacen = null) // ⬅️ nuevo
        {
            if (string.IsNullOrWhiteSpace(tipo))
                return BadRequest(new { message = "El campo 'tipo' es obligatorio." });

            var tipoMap = tipo.ToUpper() switch
            {
                "INGRESO" => "I",
                "EGRESO" => "E",
                "TRANSFERENCIA" => "T",
                _ => ""
            };

            var query = _context.MovimientosCabeceras
                .Include(m => m.IdAlmacenNavigation)
                .Where(m => m.Tipo == tipoMap);

            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            var userIdClaim = User.FindFirst("userId")?.Value;

            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Usuario no autorizado." });

                var userAlmacen = await _context.Usuarios
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
                    .FirstOrDefaultAsync();

                if (!userAlmacen.HasValue)
                    return Ok(new { TotalItems = 0, TotalPages = 0, Data = new List<MovimientoGuiaDTO>() });

                query = query.Where(m => m.IdAlmacen == userAlmacen.Value);
            }
            else if (idAlmacen.HasValue && idAlmacen.Value > 0)
            {
                query = query.Where(m => m.IdAlmacen == idAlmacen.Value); // ⬅️ filtra por almacén para admin
            }

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Motivo.Contains(search) || m.Descripcion.Contains(search));

            if (!string.IsNullOrWhiteSpace(motivo))
                query = query.Where(m => m.Motivo == motivo);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var lista = await query
                .OrderByDescending(m => m.IdMovimiento)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MovimientoGuiaDTO
                {
                    IdMovimiento = m.IdMovimiento,
                    Tipo = m.Tipo,
                    Motivo = m.Motivo,
                    Fecha = m.Fecha.HasValue ? m.Fecha.Value.ToDateTime(TimeOnly.MinValue) : m.FechaHoraRegistro,
                    Descripcion = m.Descripcion,
                    IdAlmacen = m.IdAlmacen,
                    NombreAlmacen = m.IdAlmacenNavigation.Nombre,
                    IdProveedor = m.IdProveedor,
                    IdAlmacenDestinoOrigen = m.IdAlmacenDestinoOrigen,
                    IdUsuario = m.IdUsuario,
                    Estado = m.Estado,
                    IdUsuarioConfirma = m.IdUsuarioConfirma,
                    FechaHoraConfirma = m.FechaHoraConfirma
                })
            .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                Data = lista
            });
        }

        [HttpPut("{id}/confirmar")]
        public async Task<IActionResult> ConfirmarTransferencia(int id)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            // 1) Trae el INGRESO (destino) que se está confirmando
            var ingreso = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .FirstOrDefaultAsync(m => m.IdMovimiento == id);

            if (ingreso == null)
                return NotFound(new { mensaje = "Movimiento no encontrado" });

            if (!string.Equals(ingreso.Motivo?.Trim(), "TRANSFERENCIA INGRESO", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "Solo se confirman transferencias de ingreso" });

            if (!string.Equals(ingreso.Estado?.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "El movimiento debe estar en estado 'Emitido'" });

            // 2) Ubica la guía espejo de EGRESO (origen) pendiente
            var egreso = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .Where(m =>
                    m.Tipo == "T" &&
                    m.Motivo.Trim() == "TRANSFERENCIA EGRESO" &&
                    m.Estado.Trim() == "E" &&
                    m.IdAlmacen == ingreso.IdAlmacenDestinoOrigen && // ORIGEN
                    m.IdAlmacenDestinoOrigen == ingreso.IdAlmacen    // DESTINO
                )
                .OrderByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();

            if (egreso == null)
                return BadRequest(new { mensaje = "No se encontró la guía de TRANSFERENCIA EGRESO asociada y pendiente." });

            // 🔒 valida que el usuario logueado pertenezca al almacén destino o sea administrador
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { mensaje = "Usuario no autorizado" });

            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
            {
                var userAlmacen = await _context.Usuarios
                    .Where(u => u.IdUsuario == userId)
                    .Select(u => u.IdPersonalNavigation != null ? (int?)u.IdPersonalNavigation.IdAlmacen : null)
                    .FirstOrDefaultAsync();

                if (!userAlmacen.HasValue || userAlmacen.Value != ingreso.IdAlmacen)
                    return Forbid();
            }

            // 3) Marcar ambas como confirmadas (no se persiste si el SP falla porque hay tx)
            var fechaConf = DateTime.Now;

            ingreso.IdUsuarioConfirma = userId;
            ingreso.FechaHoraConfirma = fechaConf;
            ingreso.Estado = "C";

            egreso.IdUsuarioConfirma = userId;
            egreso.FechaHoraConfirma = fechaConf;
            egreso.Estado = "C";

            await _context.SaveChangesAsync();

            // 4) Actualiza stock y registra en kardex: resta en ORIGEN y suma en DESTINO
            foreach (var detalle in egreso.MovimientosDetalles)
            {
                var stockOrigen = await _context.StockAlmacens
                    .FirstOrDefaultAsync(s => s.IdAlmacen == egreso.IdAlmacen && s.IdArticulo == detalle.IdArticulo);
                if (stockOrigen == null || stockOrigen.Stock < detalle.Cantidad)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mensaje = $"Stock insuficiente en almacén de origen para el artículo {detalle.IdArticulo}" });
                }

                var saldoInicialOrigen = stockOrigen.Stock;
                stockOrigen.Stock -= detalle.Cantidad;

                _context.Kardices.Add(new Kardex
                {
                    IdAlmacen = egreso.IdAlmacen,
                    IdArticulo = detalle.IdArticulo,
                    TipoMovimiento = "E",
                    Fecha = fechaConf,
                    SaldoInicial = saldoInicialOrigen,
                    Cantidad = detalle.Cantidad,
                    SaldoFinal = saldoInicialOrigen - detalle.Cantidad,
                    Valor = detalle.Valor,
                    Origen = $"TRANSFERENCIA EGRESO Nro: {egreso.IdMovimiento}"
                });

                var stockDestino = _context.StockAlmacens.Local
                    .FirstOrDefault(s => s.IdAlmacen == ingreso.IdAlmacen && s.IdArticulo == detalle.IdArticulo);

                if (stockDestino == null)
                {
                    stockDestino = await _context.StockAlmacens
                        .FirstOrDefaultAsync(s => s.IdAlmacen == ingreso.IdAlmacen && s.IdArticulo == detalle.IdArticulo);
                }

                var saldoInicialDestino = stockDestino?.Stock ?? 0;

                if (stockDestino == null)
                {
                    stockDestino = new StockAlmacen
                    {
                        IdAlmacen = ingreso.IdAlmacen,
                        IdArticulo = detalle.IdArticulo,
                        Stock = 0,
                        StockMinimo = 0
                    };
                    _context.StockAlmacens.Add(stockDestino);
                }

                stockDestino.Stock += detalle.Cantidad;

                _context.Kardices.Add(new Kardex
                {
                    IdAlmacen = ingreso.IdAlmacen,
                    IdArticulo = detalle.IdArticulo,
                    TipoMovimiento = "I",
                    Fecha = fechaConf,
                    SaldoInicial = saldoInicialDestino,
                    Cantidad = detalle.Cantidad,
                    SaldoFinal = saldoInicialDestino + detalle.Cantidad,
                    Valor = detalle.Valor,
                    Origen = $"TRANSFERENCIA INGRESO Nro: {ingreso.IdMovimiento}"
                });
            }

            await _context.SaveChangesAsync();

            await tx.CommitAsync();
            return Ok(new { mensaje = "✅ Transferencia confirmada: se descontó en ORIGEN y se ingresó en DESTINO." });
        }

        // ✅ GET: api/movimientosguias/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovimiento(int id)
        {
            var cabecera = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .Include(m => m.IdAlmacenNavigation)
                .FirstOrDefaultAsync(m => m.IdMovimiento == id);

            if (cabecera == null) return NotFound();

            var dto = new MovimientoGuiaDTO
            {
                IdMovimiento = cabecera.IdMovimiento,
                Tipo = cabecera.Tipo,
                Motivo = cabecera.Motivo,
                Fecha = cabecera.Fecha.HasValue ? cabecera.Fecha.Value.ToDateTime(TimeOnly.MinValue) : cabecera.FechaHoraRegistro,
                Descripcion = cabecera.Descripcion,
                IdAlmacen = cabecera.IdAlmacen,
                NombreAlmacen = cabecera.IdAlmacenNavigation?.Nombre ?? string.Empty,
                IdProveedor = cabecera.IdProveedor,
                IdAlmacenDestinoOrigen = cabecera.IdAlmacenDestinoOrigen,
                IdOc = cabecera.IdOc,
                IdUsuario = cabecera.IdUsuario,
                Estado = cabecera.Estado,
                Detalles = cabecera.MovimientosDetalles.Select(d => new MovimientoDetalleDTO
                {
                    IdMovimiento = d.IdMovimiento,
                    Item = d.Item,
                    IdArticulo = d.IdArticulo,
                    DescripcionArticulo = d.DescripcionArticulo,
                    Cantidad = d.Cantidad,
                    Valor = d.Valor
                }).ToList()
            };

            return Ok(dto);
        }

        // ✅ POST: api/movimientosguias
        [HttpPost]
        public async Task<IActionResult> CrearMovimiento([FromBody] MovimientoGuiaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var movimiento = new MovimientosCabecera
                {
                    IdAlmacen = dto.IdAlmacen,
                    Tipo = dto.Tipo,
                    Motivo = dto.Motivo,
                    Fecha = DateOnly.FromDateTime(dto.Fecha == default ? DateTime.Now : dto.Fecha),
                    Descripcion = dto.Descripcion,
                    IdProveedor = dto.IdProveedor,
                    IdAlmacenDestinoOrigen = dto.IdAlmacenDestinoOrigen,
                    IdOc = dto.IdOc,
                    IdUsuario = dto.IdUsuario,
                    FechaHoraRegistro = DateTime.Now,
                    Estado = dto.Estado
                };

                _context.MovimientosCabeceras.Add(movimiento);
                await _context.SaveChangesAsync(); // Genera IdMovimiento

                int item = 1;
                foreach (var d in dto.Detalles)
                {
                    _context.MovimientosDetalles.Add(new MovimientosDetalle
                    {
                        IdMovimiento = movimiento.IdMovimiento,
                        Item = item++,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        Cantidad = d.Cantidad,
                        Valor = d.Valor
                    });
                }

                await _context.SaveChangesAsync();

                // Si es una transferencia, registrar también el ingreso en el almacén destino
                var motivoNormalizado = dto.Motivo?.Trim() ?? string.Empty;
                var esTransferencia = string.Equals(dto.Tipo?.Trim(), "T", StringComparison.OrdinalIgnoreCase);
                var motivoTransferenciaEgreso = "TRANSFERENCIA EGRESO";
                var esTransferenciaEgreso = esTransferencia
                    && string.Equals(motivoNormalizado, motivoTransferenciaEgreso, StringComparison.OrdinalIgnoreCase);

                if (esTransferencia && (!dto.IdAlmacenDestinoOrigen.HasValue || dto.IdAlmacenDestinoOrigen.Value <= 0))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { mensaje = "Debe indicar el almacén destino para registrar una transferencia." });
                }

                if (esTransferencia)
                {
                    if (!esTransferenciaEgreso)
                    {
                        motivoNormalizado = motivoTransferenciaEgreso;
                        dto.Motivo = motivoTransferenciaEgreso;
                    }

                    var ingreso = new MovimientosCabecera
                    {
                        IdAlmacen = dto.IdAlmacenDestinoOrigen.Value,
                        Tipo = "T",
                        Motivo = "TRANSFERENCIA INGRESO",
                        Fecha = DateOnly.FromDateTime(dto.Fecha == default ? DateTime.Now : dto.Fecha),
                        Descripcion = dto.Descripcion,
                        IdProveedor = dto.IdProveedor,
                        IdAlmacenDestinoOrigen = dto.IdAlmacen,
                        IdOc = dto.IdOc,
                        IdUsuario = dto.IdUsuario,
                        FechaHoraRegistro = DateTime.Now,
                        Estado = dto.Estado
                    };

                    _context.MovimientosCabeceras.Add(ingreso);
                    await _context.SaveChangesAsync();

                    int itemIn = 1;
                    foreach (var d in dto.Detalles)
                    {
                        _context.MovimientosDetalles.Add(new MovimientosDetalle
                        {
                            IdMovimiento = ingreso.IdMovimiento,
                            Item = itemIn++,
                            IdArticulo = d.IdArticulo,
                            DescripcionArticulo = d.DescripcionArticulo,
                            Cantidad = d.Cantidad,
                            Valor = d.Valor
                        });
                    }

                    await _context.SaveChangesAsync();
                }

                // Actualizar stock y registrar en kardex para ingresos o egresos directos
                if (dto.Tipo == "I" || dto.Tipo == "E")
                {
                    foreach (var d in dto.Detalles)
                    {
                        var stock = await _context.StockAlmacens
                            .FirstOrDefaultAsync(s => s.IdAlmacen == dto.IdAlmacen && s.IdArticulo == d.IdArticulo);

                        var saldoInicial = stock?.Stock ?? 0;

                        if (dto.Tipo == "I")
                        {
                            if (stock == null)
                            {
                                stock = new StockAlmacen
                                {
                                    IdAlmacen = dto.IdAlmacen,
                                    IdArticulo = d.IdArticulo,
                                    Stock = 0,
                                    StockMinimo = 0
                                };
                                _context.StockAlmacens.Add(stock);
                            }

                            stock.Stock += d.Cantidad;

                            _context.Kardices.Add(new Kardex
                            {
                                IdAlmacen = dto.IdAlmacen,
                                IdArticulo = d.IdArticulo,
                                TipoMovimiento = "I",
                                Fecha = DateTime.Now,
                                SaldoInicial = saldoInicial,
                                Cantidad = d.Cantidad,
                                SaldoFinal = saldoInicial + d.Cantidad,
                                Valor = d.Valor,
                                Origen = $"{dto.Motivo} Nro: {movimiento.IdMovimiento}"
                            });
                        }
                        else // Egreso
                        {
                            if (stock == null || stock.Stock < d.Cantidad)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { mensaje = $"Stock insuficiente para el artículo {d.IdArticulo} en el almacén" });
                            }

                            stock.Stock -= d.Cantidad;

                            _context.Kardices.Add(new Kardex
                            {
                                IdAlmacen = dto.IdAlmacen,
                                IdArticulo = d.IdArticulo,
                                TipoMovimiento = "E",
                                Fecha = DateTime.Now,
                                SaldoInicial = saldoInicial,
                                Cantidad = d.Cantidad,
                                SaldoFinal = saldoInicial - d.Cantidad,
                                Valor = d.Valor,
                                Origen = $"{dto.Motivo} Nro: {movimiento.IdMovimiento}"
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { mensaje = "Movimiento registrado", id = movimiento.IdMovimiento });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error: {ex.Message}");
                return StatusCode(500, "Error al guardar movimiento");
            }
        }

        // ✅ PUT: api/movimientos-guias/{id}/anular
        [HttpPut("{id}/anular")]
        public async Task<IActionResult> AnularGuia(int id)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var movimiento = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .FirstOrDefaultAsync(m => m.IdMovimiento == id);

            if (movimiento == null)
                return NotFound(new { mensaje = "Movimiento no encontrado" });

            var estadoOriginal = movimiento.Estado?.Trim() ?? string.Empty;
            var motivoNormalizado = movimiento.Motivo?.Trim() ?? string.Empty;

            if (string.Equals(estadoOriginal, "A", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "La guía ya está anulada" });

            if (!string.Equals(estadoOriginal, "E", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "Solo se pueden anular guías emitidas" });

            var esTransferencia = string.Equals(motivoNormalizado, "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(motivoNormalizado, "TRANSFERENCIA INGRESO", StringComparison.OrdinalIgnoreCase);

            movimiento.Estado = "A";

            var fecha = DateTime.Now;

            bool DebeRevertirStock(bool esTransferenciaMovimiento, string estadoAnterior)
                => !esTransferenciaMovimiento || !string.Equals(estadoAnterior, "E", StringComparison.OrdinalIgnoreCase);

            async Task AjustarStockYkardex(MovimientosCabecera mov)
            {
                bool esIngreso = mov.Tipo == "I" || (mov.Tipo == "T" && mov.Motivo.Trim() == "TRANSFERENCIA INGRESO");
                bool esEgreso = mov.Tipo == "E" || (mov.Tipo == "T" && mov.Motivo.Trim() == "TRANSFERENCIA EGRESO");

                foreach (var detalle in mov.MovimientosDetalles)
                {
                    var stock = await _context.StockAlmacens
                        .FirstOrDefaultAsync(s => s.IdAlmacen == mov.IdAlmacen && s.IdArticulo == detalle.IdArticulo);

                    if (stock == null)
                    {
                        stock = new StockAlmacen
                        {
                            IdAlmacen = mov.IdAlmacen,
                            IdArticulo = detalle.IdArticulo,
                            Stock = 0,
                            StockMinimo = 0
                        };
                        _context.StockAlmacens.Add(stock);
                    }

                    var saldoInicial = stock.Stock;

                    if (esIngreso)
                    {
                        stock.Stock -= detalle.Cantidad;

                        _context.Kardices.Add(new Kardex
                        {
                            IdAlmacen = mov.IdAlmacen,
                            IdArticulo = detalle.IdArticulo,
                            TipoMovimiento = "E",
                            Fecha = fecha,
                            SaldoInicial = saldoInicial,
                            Cantidad = detalle.Cantidad,
                            SaldoFinal = stock.Stock,
                            Valor = detalle.Valor,
                            Origen = $"ANULACIÓN {mov.Motivo} Nro: {mov.IdMovimiento}"
                        });
                    }
                    else if (esEgreso)
                    {
                        stock.Stock += detalle.Cantidad;

                        _context.Kardices.Add(new Kardex
                        {
                            IdAlmacen = mov.IdAlmacen,
                            IdArticulo = detalle.IdArticulo,
                            TipoMovimiento = "I",
                            Fecha = fecha,
                            SaldoInicial = saldoInicial,
                            Cantidad = detalle.Cantidad,
                            SaldoFinal = stock.Stock,
                            Valor = detalle.Valor,
                            Origen = $"ANULACIÓN {mov.Motivo} Nro: {mov.IdMovimiento}"
                        });
                    }
                }
            }

            if (DebeRevertirStock(esTransferencia, estadoOriginal))
            {
                await AjustarStockYkardex(movimiento);
            }

            MovimientosCabecera? espejo = null;

            if (esTransferencia)
            {
                var motivoEspejo = string.Equals(motivoNormalizado, "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase)
                    ? "TRANSFERENCIA INGRESO"
                    : "TRANSFERENCIA EGRESO";

                espejo = await _context.MovimientosCabeceras
                    .Include(m => m.MovimientosDetalles)
                    .Where(m =>
                        m.Tipo == "T" &&
                        m.Motivo.Trim() == motivoEspejo &&
                        m.Estado.Trim() == "E" &&
                        m.IdAlmacen == movimiento.IdAlmacenDestinoOrigen &&
                        m.IdAlmacenDestinoOrigen == movimiento.IdAlmacen)
                    .OrderByDescending(m => m.IdMovimiento)
                    .FirstOrDefaultAsync();

                if (espejo != null)
                {
                    var estadoOriginalEspejo = espejo.Estado?.Trim() ?? string.Empty;
                    var espejoEsTransferencia = string.Equals(espejo.Motivo?.Trim(), "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(espejo.Motivo?.Trim(), "TRANSFERENCIA INGRESO", StringComparison.OrdinalIgnoreCase);

                    espejo.Estado = "A";
                    if (DebeRevertirStock(espejoEsTransferencia, estadoOriginalEspejo))
                    {
                        await AjustarStockYkardex(espejo);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            if (esTransferencia && espejo == null)
                return Ok(new { mensaje = "⚠️ Guía anulada, pero no se encontró guía espejo." });

            if (esTransferencia && espejo != null)
                return Ok(new { mensaje = "✅ Guía y guía espejo anuladas correctamente" });

            return Ok(new { mensaje = "✅ Guía anulada correctamente" });
        }

        // ✅ PUT: api/movimientosguias/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarMovimiento(int id, MovimientoGuiaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var movimiento = await _context.MovimientosCabeceras
                    .Include(m => m.MovimientosDetalles)
                    .FirstOrDefaultAsync(m => m.IdMovimiento == id);

                if (movimiento == null) return NotFound();

                // Actualizar cabecera
                movimiento.Motivo = dto.Motivo;
                movimiento.Descripcion = dto.Descripcion;
                movimiento.IdProveedor = dto.IdProveedor;
                movimiento.IdAlmacen = dto.IdAlmacen;
                movimiento.IdAlmacenDestinoOrigen = dto.IdAlmacenDestinoOrigen;
                movimiento.Estado = dto.Estado;

                // Eliminar detalles anteriores
                _context.MovimientosDetalles.RemoveRange(movimiento.MovimientosDetalles);

                // Agregar nuevos detalles
                int item = 1;
                foreach (var d in dto.Detalles)
                {
                    _context.MovimientosDetalles.Add(new MovimientosDetalle
                    {
                        IdMovimiento = movimiento.IdMovimiento,
                        Item = item++,
                        IdArticulo = d.IdArticulo,
                        DescripcionArticulo = d.DescripcionArticulo,
                        Cantidad = d.Cantidad,
                        Valor = d.Valor
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Movimiento actualizado" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"❌ Error: {ex.Message}");
                return StatusCode(500, "Error al actualizar movimiento");
            }
        }

        [HttpPost("registrar-con-sp")]
        public async Task<IActionResult> RegistrarConSP([FromBody] MovimientoGuiaDTO dto)
        {
            try
            {
                // Convertir dto a DataTable
                var cabecera = new MovimientoCabeceraOnlyDTO
                {
                    IdMovimiento = dto.IdMovimiento,
                    IdAlmacen = dto.IdAlmacen,
                    Tipo = dto.Tipo,
                    Motivo = dto.Motivo,
                    Fecha = dto.Fecha == default ? DateTime.Now : dto.Fecha,
                    Descripcion = dto.Descripcion,
                    IdProveedor = dto.IdProveedor,
                    IdAlmacenDestinoOrigen = dto.IdAlmacenDestinoOrigen,
                    IdOc = dto.IdOc,
                    IdUsuario = dto.IdUsuario,
                    FechaHoraRegistro = DateTime.Now,
                    IdGuiaRemision = null, // o un valor real si aplica
                    IdUsuarioConfirma = dto.IdUsuarioConfirma,
                    FechaHoraConfirma = dto.FechaHoraConfirma,
                    Estado = dto.Estado
                };

                var dtCabecera = DataTableHelper.ToDataTable(new List<MovimientoCabeceraOnlyDTO> { cabecera }, "Almacen.MovimientosCabeceraType");
                var dtDetalle = DataTableHelper.ToDataTable(dto.Detalles, "Almacen.MovimientosDetalleType");

                using var conn = new SqlConnection(_context.Database.GetConnectionString());
                using var cmd = new SqlCommand("Almacen.usp_InsUpd_MovimientoAlmacen", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@tblCabecera", dtCabecera);
                cmd.Parameters["@tblCabecera"].SqlDbType = SqlDbType.Structured;
                cmd.Parameters["@tblCabecera"].TypeName = "Almacen.MovimientosCabeceraType";

                cmd.Parameters.AddWithValue("@tblDetalle", dtDetalle);
                cmd.Parameters["@tblDetalle"].SqlDbType = SqlDbType.Structured;
                cmd.Parameters["@tblDetalle"].TypeName = "Almacen.MovimientosDetalleType";

                await conn.OpenAsync();
                foreach (DataColumn col in dtCabecera.Columns)
                {
                    Console.WriteLine($"{col.ColumnName}: {col.DataType}");
                }
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { mensaje = "Movimiento registrado con SP correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("❌ Inner: " + ex.InnerException.Message);
                return StatusCode(500, $"❌ Error al ejecutar SP: {ex.Message}");
            }
        }
    }
}
