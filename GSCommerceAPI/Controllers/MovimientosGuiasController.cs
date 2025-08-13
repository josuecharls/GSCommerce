using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerce.Client.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using GSCommerceAPI.Helpers;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/movimientos-guias")]
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

            if (idAlmacen.HasValue && idAlmacen.Value > 0)
                query = query.Where(m => m.IdAlmacen == idAlmacen.Value); // ⬅️ filtra por almacén

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Motivo.Contains(search) || m.Descripcion.Contains(search));

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

            // 🔒 (opcional) valida que el usuario logueado pertenezca al almacén destino (ingreso.IdAlmacen)

            // 3) Marcar ambas como confirmadas (no se persiste si el SP falla porque hay tx)
            var userId = 1; // TODO: obtener desde el token
            var fechaConf = DateTime.Now;

            ingreso.IdUsuarioConfirma = userId;
            ingreso.FechaHoraConfirma = fechaConf;
            ingreso.Estado = "C";

            egreso.IdUsuarioConfirma = userId;
            egreso.FechaHoraConfirma = fechaConf;
            egreso.Estado = "C";

            await _context.SaveChangesAsync();

            // 4) Construir TVPs para el SP: una fila E (origen) + una fila I (destino)
            var cabeceras = new List<MovimientoCabeceraOnlyDTO>
    {
        // EGRESO (origen) → resta stock
        new MovimientoCabeceraOnlyDTO
        {
            IdMovimiento = egreso.IdMovimiento,
            IdAlmacen = egreso.IdAlmacen, // ORIGEN
            Tipo = "E",
            Motivo = "TRANSFERENCIA EGRESO",
            Fecha = egreso.Fecha?.ToDateTime(TimeOnly.MinValue) ?? egreso.FechaHoraRegistro,
            Descripcion = egreso.Descripcion,
            IdProveedor = egreso.IdProveedor,
            IdAlmacenDestinoOrigen = egreso.IdAlmacenDestinoOrigen, // DESTINO
            IdOc = egreso.IdOc,
            IdUsuario = egreso.IdUsuario,
            FechaHoraRegistro = egreso.FechaHoraRegistro,
            IdGuiaRemision = egreso.IdGuiaRemision,
            IdUsuarioConfirma = egreso.IdUsuarioConfirma,
            FechaHoraConfirma = egreso.FechaHoraConfirma,
            Estado = "C"
        },

        // INGRESO (destino) → suma stock
        new MovimientoCabeceraOnlyDTO
        {
            IdMovimiento = ingreso.IdMovimiento,
            IdAlmacen = ingreso.IdAlmacen, // DESTINO
            Tipo = "I",
            Motivo = "TRANSFERENCIA INGRESO",
            Fecha = ingreso.Fecha?.ToDateTime(TimeOnly.MinValue) ?? ingreso.FechaHoraRegistro,
            Descripcion = ingreso.Descripcion,
            IdProveedor = ingreso.IdProveedor,
            IdAlmacenDestinoOrigen = ingreso.IdAlmacenDestinoOrigen, // ORIGEN
            IdOc = ingreso.IdOc,
            IdUsuario = ingreso.IdUsuario,
            FechaHoraRegistro = ingreso.FechaHoraRegistro,
            IdGuiaRemision = ingreso.IdGuiaRemision,
            IdUsuarioConfirma = ingreso.IdUsuarioConfirma,
            FechaHoraConfirma = ingreso.FechaHoraConfirma,
            Estado = "C"
        }
    };

            var dtCabecera = DataTableHelper.ToDataTable(cabeceras, "Almacen.MovimientosCabeceraType");

            // Detalles para ambas guías
            var detTodo = new List<MovimientoDetalleDTO>();

            detTodo.AddRange(egreso.MovimientosDetalles.Select(d => new MovimientoDetalleDTO
            {
                IdMovimiento = d.IdMovimiento,
                Item = d.Item,
                IdArticulo = d.IdArticulo,
                DescripcionArticulo = d.DescripcionArticulo,
                Cantidad = d.Cantidad,
                Valor = d.Valor
            }));

            detTodo.AddRange(ingreso.MovimientosDetalles.Select(d => new MovimientoDetalleDTO
            {
                IdMovimiento = d.IdMovimiento,
                Item = d.Item,
                IdArticulo = d.IdArticulo,
                DescripcionArticulo = d.DescripcionArticulo,
                Cantidad = d.Cantidad,
                Valor = d.Valor
            }));

            var dtDetalle = DataTableHelper.ToDataTable(detTodo, "Almacen.MovimientosDetalleType");

            // 5) Ejecutar SP en una sola llamada
            using var conn = new SqlConnection(_context.Database.GetConnectionString());
            using var cmd = new SqlCommand("Almacen.usp_InsUpd_MovimientoAlmacen", conn)
            { CommandType = CommandType.StoredProcedure };

            var pCab = cmd.Parameters.AddWithValue("@tblCabecera", dtCabecera);
            pCab.SqlDbType = SqlDbType.Structured;
            pCab.TypeName = "Almacen.MovimientosCabeceraType";

            var pDet = cmd.Parameters.AddWithValue("@tblDetalle", dtDetalle);
            pDet.SqlDbType = SqlDbType.Structured;
            pDet.TypeName = "Almacen.MovimientosDetalleType";

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            await tx.CommitAsync();

            return Ok(new { mensaje = "✅ Transferencia confirmada: se descontó en ORIGEN y se ingresó en DESTINO." });
        }

        // ✅ GET: api/movimientosguias/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetMovimiento(int id)
        {
            var cabecera = await _context.MovimientosCabeceras
                .Include(m => m.MovimientosDetalles)
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
                if (dto.Motivo == "TRANSFERENCIA EGRESO" && dto.IdAlmacenDestinoOrigen.HasValue)
                {
                    var ingreso = new MovimientosCabecera
                    {
                        IdAlmacen = dto.IdAlmacenDestinoOrigen.Value,
                        Tipo = dto.Tipo,
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
            var movimiento = await _context.MovimientosCabeceras
                .FirstOrDefaultAsync(m => m.IdMovimiento == id);

            if (movimiento == null)
                return NotFound(new { mensaje = "Movimiento no encontrado" });

            if (movimiento.Estado == "A")
                return BadRequest(new { mensaje = "La guía ya está anulada" });

            if (movimiento.Estado != "E")
                return BadRequest(new { mensaje = "Solo se pueden anular guías emitidas" });

            movimiento.Estado = "A";
            await _context.SaveChangesAsync();

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
