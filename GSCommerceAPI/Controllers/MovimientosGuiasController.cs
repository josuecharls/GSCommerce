using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using GSCommerceAPI.Helpers;
using GSCommerceAPI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/movimientos-guias")]
    [Authorize(Roles = "ADMINISTRADOR,CAJERO,ALMACEN")]
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
                .Where(m => m.Tipo == tipoMap)
                // Ingresos por NC se mantienen en BD pero no se muestran en el listado de guías
                .Where(m => m.Motivo != "NC"
                    && (m.Descripcion == null || !m.Descripcion.StartsWith("Ingreso por NC")));

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

            if (!ingreso.IdMovimientoEspejo.HasValue || ingreso.IdMovimientoEspejo.Value <= 0)
                return BadRequest(new { mensaje = "La guía de ingreso no tiene egreso espejo enlazado. No se confirma para evitar cruzar transferencias." });

            // 2) Ubica la guía espejo de EGRESO (origen) y valida que sea exactamente ese par
            var egreso = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
                .FirstOrDefaultAsync(m => m.IdMovimiento == ingreso.IdMovimientoEspejo);

            if (egreso == null)
                return BadRequest(new { mensaje = "No se encontró la guía de TRANSFERENCIA EGRESO asociada y pendiente." });

            if (!string.Equals(egreso.Motivo?.Trim(), "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = $"El espejo #{egreso.IdMovimiento} no es una TRANSFERENCIA EGRESO. No se confirma." });

            if (!string.Equals(egreso.Estado?.Trim(), "E", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = $"El egreso espejo #{egreso.IdMovimiento} no está pendiente (estado '{egreso.Estado}'). No se confirma." });

            if (egreso.IdAlmacen != ingreso.IdAlmacenDestinoOrigen
                || egreso.IdAlmacenDestinoOrigen != ingreso.IdAlmacen)
            {
                return BadRequest(new { mensaje = $"El egreso #{egreso.IdMovimiento} no corresponde a la ruta de almacenes de la guía #{ingreso.IdMovimiento}. No se confirma." });
            }

            if (egreso.IdMovimientoEspejo.HasValue && egreso.IdMovimientoEspejo.Value != ingreso.IdMovimiento)
            {
                return BadRequest(new { mensaje = $"El egreso #{egreso.IdMovimiento} está enlazado a otra guía (#{egreso.IdMovimientoEspejo}), no a la #{ingreso.IdMovimiento}. No se confirma." });
            }

            if (!DetallesTransferenciaCoinciden(ingreso.MovimientosDetalles, egreso.MovimientosDetalles, out var detalleMismatch))
            {
                return BadRequest(new
                {
                    mensaje = $"El detalle de TRANSFERENCIA INGRESO Nro: {ingreso.IdMovimiento} no coincide con TRANSFERENCIA EGRESO Nro: {egreso.IdMovimiento}. {detalleMismatch} No se movió stock ni kardex."
                });
            }

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

            if (!egreso.IdMovimientoEspejo.HasValue)
                egreso.IdMovimientoEspejo = ingreso.IdMovimiento;

            ingreso.IdUsuarioConfirma = userId;
            ingreso.FechaHoraConfirma = fechaConf;
            ingreso.Estado = "C";

            egreso.IdUsuarioConfirma = userId;
            egreso.FechaHoraConfirma = fechaConf;
            egreso.Estado = "C";

            await _context.SaveChangesAsync();

            // Tienda de donde viene = almacén del egreso (origen)
            var idAlmacenOrigen = egreso.IdAlmacen > 0
                ? egreso.IdAlmacen
                : (ingreso.IdAlmacenDestinoOrigen ?? 0);
            var idAlmacenDestino = ingreso.IdAlmacen;

            var nombresAlmacen = await _context.Almacens
                .AsNoTracking()
                .Where(a => a.IdAlmacen == idAlmacenOrigen || a.IdAlmacen == idAlmacenDestino)
                .Select(a => new { a.IdAlmacen, a.Nombre })
                .ToListAsync();

            var nombreAlmacenOrigen = nombresAlmacen
                .FirstOrDefault(a => a.IdAlmacen == idAlmacenOrigen)?.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombreAlmacenOrigen))
                nombreAlmacenOrigen = $"Almacén {idAlmacenOrigen}";

            var nombreAlmacenDestino = nombresAlmacen
                .FirstOrDefault(a => a.IdAlmacen == idAlmacenDestino)?.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombreAlmacenDestino))
                nombreAlmacenDestino = $"Almacén {idAlmacenDestino}";

            var textoKardexIngreso = KardexTransferenciaDescripcion.FormatearIngreso(
                ingreso.IdMovimiento, nombreAlmacenOrigen);
            var textoKardexEgreso = KardexTransferenciaDescripcion.FormatearEgreso(
                egreso.IdMovimiento, nombreAlmacenDestino);

            // 4) Stock y kardex: agrupar por código para no descontar dos veces el mismo artículo
            //    (si había filas duplicadas 3+2 con stock 4, línea a línea fallaba al confirmar)
            var detallesConfirmacion = ingreso.MovimientosDetalles
                .GroupBy(d => (
                    Articulo: (d.IdArticulo ?? string.Empty).Trim(),
                    Variante: IdVarianteUtil.Normalizar(d.IdVariante)))
                .Select(g => new
                {
                    IdArticulo = g.Key.Articulo,
                    IdVariante = g.Key.Variante,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Valor = g.First().Valor
                })
                .Where(d => !string.IsNullOrWhiteSpace(d.IdArticulo) && d.Cantidad > 0)
                .ToList();

            foreach (var detalle in detallesConfirmacion)
            {
                var stockOrigen = await IdVarianteUtil.BuscarStockAlmacenAsync(
                    _context.StockAlmacens, egreso.IdAlmacen, detalle.IdArticulo);
                if (stockOrigen == null || stockOrigen.Stock < detalle.Cantidad)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { mensaje = $"Stock insuficiente en almacén de origen para el artículo {detalle.IdArticulo} (requerido: {detalle.Cantidad}, disponible: {stockOrigen?.Stock ?? 0})" });
                }

                var saldoInicialOrigen = stockOrigen.Stock;
                stockOrigen.Stock -= detalle.Cantidad;

                _context.Kardices.Add(new Kardex
                {
                    IdAlmacen = egreso.IdAlmacen,
                    IdArticulo = detalle.IdArticulo,
                    IdVariante = detalle.IdVariante,
                    TipoMovimiento = "E",
                    Fecha = fechaConf,
                    SaldoInicial = saldoInicialOrigen,
                    Cantidad = detalle.Cantidad,
                    SaldoFinal = saldoInicialOrigen - detalle.Cantidad,
                    Valor = detalle.Valor,
                    Origen = textoKardexEgreso
                });

                var stockDestino = IdVarianteUtil.BuscarStockAlmacenLocal(
                    _context.StockAlmacens.Local,
                    ingreso.IdAlmacen,
                    detalle.IdArticulo);

                if (stockDestino == null)
                {
                    stockDestino = await IdVarianteUtil.BuscarStockAlmacenAsync(
                        _context.StockAlmacens, ingreso.IdAlmacen, detalle.IdArticulo);
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
                    IdVariante = detalle.IdVariante,
                    TipoMovimiento = "I",
                    Fecha = fechaConf,
                    SaldoInicial = saldoInicialDestino,
                    Cantidad = detalle.Cantidad,
                    SaldoFinal = saldoInicialDestino + detalle.Cantidad,
                    Valor = detalle.Valor,
                    Origen = textoKardexIngreso
                });
            }

            await _context.SaveChangesAsync();

            // 5) Marcar todas las notificaciones relacionadas con esta guía como leídas
            var notificacionesRelacionadas = await _context.Notificaciones
                .Where(n => n.IdMovimiento == ingreso.IdMovimiento && !n.Leida)
                .ToListAsync();

            foreach (var notificacion in notificacionesRelacionadas)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.Now;
            }

            if (notificacionesRelacionadas.Any())
            {
                await _context.SaveChangesAsync();
            }

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
                FechaHoraRegistro = cabecera.FechaHoraRegistro,
                IdUsuarioConfirma = cabecera.IdUsuarioConfirma,
                FechaHoraConfirma = cabecera.FechaHoraConfirma,
                Detalles = cabecera.MovimientosDetalles.Select(d => new MovimientoDetalleDTO
                {
                    IdMovimiento = d.IdMovimiento,
                    Item = d.Item,
                    IdArticulo = d.IdArticulo,
                    IdVariante = d.IdVariante,
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
                // Mismo código en varias filas → una sola línea con la suma (evita pasar validación línea a línea)
                dto.Detalles = MovimientoDetalleConsolidacion.ConsolidarPorCodigo(dto.Detalles);

                var esTransferenciaInit = string.Equals(dto.Tipo?.Trim(), "T", StringComparison.OrdinalIgnoreCase);
                if (esTransferenciaInit)
                {
                    foreach (var d in dto.Detalles)
                    {
                        var stock = await IdVarianteUtil.BuscarStockAlmacenAsync(
                            _context.StockAlmacens, dto.IdAlmacen, d.IdArticulo);

                        if (stock == null || stock.Stock < d.Cantidad)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { mensaje = $"Stock insuficiente para el artículo {d.IdArticulo} en el almacén origen." });
                        }
                    }
                }

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
                        IdVariante = IdVarianteUtil.Normalizar(d.IdVariante),
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
                            IdVariante = IdVarianteUtil.Normalizar(d.IdVariante),
                            DescripcionArticulo = d.DescripcionArticulo,
                            Cantidad = d.Cantidad,
                            Valor = d.Valor
                        });
                    }

                    await _context.SaveChangesAsync();

                    // Crear notificaciones para usuarios del almacén destino
                    var usuariosDestino = await _context.Usuarios
                        .Include(u => u.IdPersonalNavigation)
                        .Where(u => 
                            u.Estado == true &&
                            u.IdPersonalNavigation != null &&
                            u.IdPersonalNavigation.IdAlmacen == dto.IdAlmacenDestinoOrigen.Value &&
                            (u.IdPersonalNavigation.Cargo == "ALMACEN" || 
                             u.IdPersonalNavigation.Cargo == "ADMINISTRADOR" ||
                             u.IdPersonalNavigation.Cargo == "CAJERO"))
                        .ToListAsync();

                    foreach (var usuario in usuariosDestino)
                    {
                        _context.Notificaciones.Add(new Notificacion
                        {
                            IdUsuario = usuario.IdUsuario,
                            IdMovimiento = ingreso.IdMovimiento,
                            Tipo = "TRANSFERENCIA",
                            Mensaje = $"Se acaba de hacer una transferencia a su almacén (Guía #{ingreso.IdMovimiento}). Por favor verifique la lista y confirme para ingresar el stock.",
                            Leida = false,
                            FechaCreacion = DateTime.Now
                        });
                    }

                    await _context.SaveChangesAsync();

                    movimiento.IdMovimientoEspejo = ingreso.IdMovimiento;
                    ingreso.IdMovimientoEspejo = movimiento.IdMovimiento;
                    await _context.SaveChangesAsync();
                }

                // Actualizar stock y registrar en kardex para ingresos o egresos directos
                if (dto.Tipo == "I" || dto.Tipo == "E")
                {
                    foreach (var d in dto.Detalles)
                    {
                        var nv = IdVarianteUtil.Normalizar(d.IdVariante);
                        var stock = await IdVarianteUtil.BuscarStockAlmacenAsync(
                            _context.StockAlmacens, dto.IdAlmacen, d.IdArticulo);

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
                                IdVariante = nv,
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
                                IdVariante = nv,
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

            // Permitir anular guías emitidas (E) o confirmadas (C)
            if (!string.Equals(estadoOriginal, "E", StringComparison.OrdinalIgnoreCase) && 
                !string.Equals(estadoOriginal, "C", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { mensaje = "Solo se pueden anular guías emitidas o confirmadas" });

            var esEgresoTransferencia = string.Equals(motivoNormalizado, "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase);
            var esIngresoTransferencia = string.Equals(motivoNormalizado, "TRANSFERENCIA INGRESO", StringComparison.OrdinalIgnoreCase);
            var esTransferencia = esEgresoTransferencia || esIngresoTransferencia;
            var confirmada = string.Equals(estadoOriginal, "C", StringComparison.OrdinalIgnoreCase)
                || movimiento.IdUsuarioConfirma.HasValue;

            if (esTransferencia)
            {
                if (esEgresoTransferencia && confirmada)
                    return BadRequest(new { mensaje = "Después de confirmar, anule desde la TRANSFERENCIA INGRESO (solo durante las primeras 72 horas)." });

                if (esIngresoTransferencia && !confirmada)
                    return BadRequest(new { mensaje = "Antes de confirmar, anule desde la TRANSFERENCIA EGRESO." });

                if (confirmada)
                {
                    var fechaConfirma = movimiento.FechaHoraConfirma;
                    if (!fechaConfirma.HasValue || DateTime.Now - fechaConfirma.Value > TimeSpan.FromHours(72))
                        return BadRequest(new { mensaje = "Han pasado más de 72 horas desde la confirmación. Esta transferencia ya no se puede anular." });
                }
            }

            movimiento.Estado = "A";

            var fecha = DateTime.Now;

            // Revertir stock si:
            // - No es transferencia (siempre revertir)
            // - Es transferencia y el estado anterior era "C" (confirmado) - revertir
            bool DebeRevertirStock(bool esTransferenciaMovimiento, string estadoAnterior)
                => !esTransferenciaMovimiento || 
                   string.Equals(estadoAnterior, "C", StringComparison.OrdinalIgnoreCase);

            async Task AjustarStockYkardex(MovimientosCabecera mov)
            {
                bool esIngreso = mov.Tipo == "I" || (mov.Tipo == "T" && mov.Motivo.Trim() == "TRANSFERENCIA INGRESO");
                bool esEgreso = mov.Tipo == "E" || (mov.Tipo == "T" && mov.Motivo.Trim() == "TRANSFERENCIA EGRESO");

                foreach (var detalle in mov.MovimientosDetalles)
                {
                    var nv = IdVarianteUtil.Normalizar(detalle.IdVariante);
                    var stock = await IdVarianteUtil.BuscarStockAlmacenAsync(
                        _context.StockAlmacens, mov.IdAlmacen, detalle.IdArticulo);

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
                            IdVariante = nv,
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
                            IdVariante = nv,
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
                espejo = await _context.MovimientosCabeceras
                    .Include(m => m.MovimientosDetalles)
                    .FirstOrDefaultAsync(m => m.IdMovimiento == movimiento.IdMovimientoEspejo);

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

                var esTransferencia = movimiento.Tipo == "T";
                var confirmada = string.Equals(movimiento.Estado?.Trim(), "C", StringComparison.OrdinalIgnoreCase)
                    || movimiento.IdUsuarioConfirma.HasValue;
                if (esTransferencia && confirmada)
                {
                    return BadRequest(new { mensaje = "No se puede editar una transferencia confirmada." });
                }

                // Si es transferencia, buscar la guía espejo
                MovimientosCabecera? guiaEspejo = null;
                if (esTransferencia)
                {
                    guiaEspejo = await _context.MovimientosCabeceras
                        .Include(m => m.MovimientosDetalles)
                        .FirstOrDefaultAsync(m => m.IdMovimiento == movimiento.IdMovimientoEspejo);

                    // Validar que la guía espejo tampoco esté confirmada
                    if (guiaEspejo != null && (guiaEspejo.IdUsuarioConfirma.HasValue
                        || string.Equals(guiaEspejo.Estado?.Trim(), "C", StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new { mensaje = "No se puede editar una transferencia cuya guía espejo está confirmada." });
                    }
                }

                // Actualizar cabecera (NO se permite cambiar Motivo, Almacén ni Proveedor)
                movimiento.Descripcion = dto.Descripcion;
                movimiento.Estado = dto.Estado;
                // Nota: Motivo, IdAlmacen, IdProveedor e IdAlmacenDestinoOrigen NO se actualizan

                dto.Detalles = MovimientoDetalleConsolidacion.ConsolidarPorCodigo(dto.Detalles);

                if (esTransferencia && string.Equals(movimiento.Motivo?.Trim(), "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var d in dto.Detalles)
                    {
                        var stock = await IdVarianteUtil.BuscarStockAlmacenAsync(
                            _context.StockAlmacens, movimiento.IdAlmacen, d.IdArticulo);
                        if (stock == null || stock.Stock < d.Cantidad)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { mensaje = $"Stock insuficiente para el artículo {d.IdArticulo} en el almacén origen." });
                        }
                    }
                }

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
                        IdVariante = IdVarianteUtil.Normalizar(d.IdVariante),
                        DescripcionArticulo = d.DescripcionArticulo,
                        Cantidad = d.Cantidad,
                        Valor = d.Valor
                    });
                }

                await _context.SaveChangesAsync();

                // Si es transferencia y existe guía espejo, sincronizar
                if (esTransferencia && guiaEspejo != null)
                {
                    // Actualizar descripción en la guía espejo
                    guiaEspejo.Descripcion = dto.Descripcion;
                    guiaEspejo.Estado = dto.Estado;

                    // Eliminar detalles anteriores de la guía espejo
                    _context.MovimientosDetalles.RemoveRange(guiaEspejo.MovimientosDetalles);

                    // Agregar los mismos detalles a la guía espejo
                    int itemEspejo = 1;
                    foreach (var d in dto.Detalles)
                    {
                        _context.MovimientosDetalles.Add(new MovimientosDetalle
                        {
                            IdMovimiento = guiaEspejo.IdMovimiento,
                            Item = itemEspejo++,
                            IdArticulo = d.IdArticulo,
                            IdVariante = IdVarianteUtil.Normalizar(d.IdVariante),
                            DescripcionArticulo = d.DescripcionArticulo,
                            Cantidad = d.Cantidad,
                            Valor = d.Valor
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Movimiento actualizado" + (esTransferencia && guiaEspejo != null ? " (guía espejo sincronizada)" : "") });
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
                var detallesParaSp = dto.Detalles.Select(d => new MovimientosDetalleTvpDTO
                {
                    IdMovimiento = d.IdMovimiento,
                    Item = d.Item,
                    IdArticulo = d.IdArticulo,
                    DescripcionArticulo = d.DescripcionArticulo,
                    Cantidad = d.Cantidad,
                    Valor = d.Valor
                }).ToList();
                var dtDetalle = DataTableHelper.ToDataTable(detallesParaSp, "Almacen.MovimientosDetalleType");

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

        private static Dictionary<(string Articulo, int? Variante), int> AgruparDetalleTransferencia(
            IEnumerable<MovimientosDetalle> detalles)
        {
            return detalles
                .GroupBy(d => (
                    Articulo: (d.IdArticulo ?? string.Empty).Trim(),
                    Variante: IdVarianteUtil.Normalizar(d.IdVariante)))
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));
        }

        private static bool DetallesTransferenciaCoinciden(
            ICollection<MovimientosDetalle> ingresoDetalles,
            ICollection<MovimientosDetalle> egresoDetalles,
            out string mismatch)
        {
            var ingreso = AgruparDetalleTransferencia(ingresoDetalles);
            var egreso = AgruparDetalleTransferencia(egresoDetalles);

            if (ingreso.Count == 0)
            {
                mismatch = "La guía de ingreso no tiene detalle.";
                return false;
            }

            if (egreso.Count == 0)
            {
                mismatch = "La guía de egreso espejo no tiene detalle.";
                return false;
            }

            var claves = ingreso.Keys.Union(egreso.Keys).ToList();
            var diferencias = new List<string>();

            foreach (var clave in claves)
            {
                ingreso.TryGetValue(clave, out var cantIn);
                egreso.TryGetValue(clave, out var cantEg);
                if (cantIn == cantEg)
                    continue;

                var variante = clave.Variante.HasValue ? $" var:{clave.Variante}" : string.Empty;
                diferencias.Add($"{clave.Articulo}{variante} ingreso={cantIn} egreso={cantEg}");
                if (diferencias.Count >= 8)
                    break;
            }

            if (diferencias.Count == 0)
            {
                mismatch = string.Empty;
                return true;
            }

            mismatch = "Diferencias: " + string.Join("; ", diferencias) + ".";
            return false;
        }
    }
}
