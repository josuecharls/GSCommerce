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
            [FromQuery] string search = "")
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m =>
                    m.Motivo.Contains(search) ||
                    m.Descripcion.Contains(search));
            }

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
                    Fecha = m.Fecha.HasValue ? m.Fecha.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,
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
            var movimiento = await _context.MovimientosCabeceras.FirstOrDefaultAsync(m => m.IdMovimiento == id);

            if (movimiento == null)
                return NotFound(new { mensaje = "Movimiento no encontrado" });

            if (movimiento.Motivo != "TRANSFERENCIA INGRESO")
                return BadRequest(new { mensaje = "Solo se pueden confirmar transferencias por ingreso" });

            if (movimiento.Estado != "E")
                return BadRequest(new { mensaje = "El movimiento debe estar en estado 'Emitido'" });

            if (movimiento.IdUsuarioConfirma != null)
                return BadRequest(new { mensaje = "El movimiento ya fue confirmado" });

            movimiento.IdUsuarioConfirma = 1; // ← Cambiar por el usuario autenticado
            movimiento.FechaHoraConfirma = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "✅ Transferencia confirmada" });
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
                Fecha = cabecera.Fecha.HasValue ? cabecera.Fecha.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,
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
                    Fecha = DateOnly.FromDateTime(dto.Fecha),
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
                    Fecha = dto.Fecha,
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
