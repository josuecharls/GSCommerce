using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngresosEgresosController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private const int MaxImageSize = 5 * 1024 * 1024;

        public IngresosEgresosController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/IngresosEgresos?idAlmacen=1&fechaInicio=2024-01-01&fechaFin=2024-01-31&naturaleza=Egreso&tipo=PAGO%20PROVEEDORES
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int? idAlmacen, [FromQuery] int? idUsuario,
            [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] string? naturaleza,
            [FromQuery] string? tipo)
        {
            var query = _context.VListadoIngresosEgresos1s.AsQueryable();

            if (idAlmacen.HasValue)
                query = query.Where(q => q.IdAlmacen == idAlmacen);

            if (idUsuario.HasValue)
                query = query.Where(q => q.IdUsuario == idUsuario);

            if (fechaInicio.HasValue || fechaFin.HasValue)
            {
                var inicio = (fechaInicio ?? DateTime.MinValue).Date;
                var finExclusivo = ((fechaFin ?? DateTime.MaxValue).Date).AddDays(1);

                query = query.Where(q => q.Fecha >= inicio && q.Fecha < finExclusivo);
            }

            if (!string.IsNullOrEmpty(naturaleza))
            {
                var nat = naturaleza.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "E";
                query = query.Where(q => q.Naturaleza == nat);
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                query = query.Where(q => q.Tipo == tipo);
            }

            var result = await query
                .OrderByDescending(q => q.Fecha)
                .ToListAsync();

            return Ok(result);
        }

        // POST: api/IngresosEgresos
        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] Models.DTOs.IngresoEgresoRegistroDTO dto)
        {
            if (dto == null)
                return BadRequest("Datos inválidos");

            var cabecera = new IngresosEgresosCabecera
            {
                IdUsuario = dto.IdUsuario,
                IdAlmacen = dto.IdAlmacen,
                Naturaleza = dto.Naturaleza.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "E",
                Tipo = dto.Tipo,
                Fecha = dto.Fecha,
                Glosa = dto.Glosa,
                Monto = dto.Monto,
                IdProveedor = dto.IdProveedor,
                IdAlmacenDestino = dto.IdAlmacenDestino,
                IdCajeroDestino = dto.IdCajeroDestino,
                IdAlmacenGasto = dto.IdAlmacenGasto,
                FechaRegistro = DateTime.Now,
                Estado = "E"
            };

            _context.IngresosEgresosCabeceras.Add(cabecera);
            await _context.SaveChangesAsync();

            foreach (var det in dto.Detalles)
            {
                byte[]? imagen = null;
                if (!string.IsNullOrEmpty(det.ImagenBase64))
                {
                    try
                    {
                        imagen = Convert.FromBase64String(det.ImagenBase64);
                    }
                    catch (FormatException)
                    {
                        return BadRequest("Imagen en formato Base64 inválido.");
                    }
                    if (imagen.Length > MaxImageSize)
                        return BadRequest("La imagen excede el tamaño máximo permitido (5 MB).");
                }

                var detalle = new IngresosEgresosDetalle
                {
                    IdIngresoEgreso = cabecera.IdIngresoEgreso,
                    Forma = det.Forma,
                    Detalle = det.Detalle,
                    Monto = det.Monto,
                    Banco = det.Banco,
                    Cuenta = det.Cuenta,
                    Imagen = imagen
                };
                _context.IngresosEgresosDetalles.Add(detalle);
            }

            await _context.SaveChangesAsync();

            // Si es un Egreso tipo TRANSFERENCIA con cajero destino, crear automáticamente el Ingreso para el cajero destino
            if (cabecera.Naturaleza == "E" && 
                cabecera.Tipo.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase) &&
                cabecera.IdCajeroDestino.HasValue)
            {
                // Obtener el almacén del cajero destino
                var cajeroDestino = await _context.Usuarios
                    .Include(u => u.IdPersonalNavigation)
                    .FirstOrDefaultAsync(u => u.IdUsuario == cabecera.IdCajeroDestino.Value);

                if (cajeroDestino?.IdPersonalNavigation != null)
                {
                    var idAlmacenDestino = cabecera.IdAlmacenDestino ?? cajeroDestino.IdPersonalNavigation.IdAlmacen;
                    
                    // Validar que tenemos un almacén válido
                    if (!idAlmacenDestino.HasValue)
                    {
                        return BadRequest("No se pudo determinar el almacén del cajero destino.");
                    }
                    
                    // Obtener el nombre del cajero origen para la glosa
                    var cajeroOrigen = await _context.Usuarios
                        .Include(u => u.IdPersonalNavigation)
                        .FirstOrDefaultAsync(u => u.IdUsuario == cabecera.IdUsuario);
                    
                    var nombreCajeroOrigen = cajeroOrigen?.IdPersonalNavigation != null
                        ? $"{cajeroOrigen.IdPersonalNavigation.Nombres} {cajeroOrigen.IdPersonalNavigation.Apellidos}".Trim()
                        : "Cajero Origen";

                    // Crear el ingreso para el cajero destino
                    var ingresoDestino = new IngresosEgresosCabecera
                    {
                        IdUsuario = cabecera.IdCajeroDestino.Value,
                        IdAlmacen = idAlmacenDestino.Value,
                        Naturaleza = "I",
                        Tipo = "TRANSFERENCIA",
                        Fecha = cabecera.Fecha,
                        Glosa = $"Transferencia recibida de {nombreCajeroOrigen}",
                        Monto = cabecera.Monto,
                        IdProveedor = null,
                        IdAlmacenDestino = cabecera.IdAlmacen,
                        IdCajeroDestino = null,
                        IdAlmacenGasto = null,
                        FechaRegistro = DateTime.Now,
                        Estado = "E"
                    };

                    _context.IngresosEgresosCabeceras.Add(ingresoDestino);
                    await _context.SaveChangesAsync();

                    // Copiar los detalles al ingreso destino
                    foreach (var det in dto.Detalles)
                    {
                        byte[]? imagenDestino = null;
                        if (!string.IsNullOrEmpty(det.ImagenBase64))
                        {
                            try
                            {
                                imagenDestino = Convert.FromBase64String(det.ImagenBase64);
                            }
                            catch (FormatException)
                            {
                                // Si hay error con la imagen, continuar sin ella
                            }
                        }

                        var detalleDestino = new IngresosEgresosDetalle
                        {
                            IdIngresoEgreso = ingresoDestino.IdIngresoEgreso,
                            Forma = det.Forma,
                            Detalle = det.Detalle,
                            Monto = det.Monto,
                            Banco = det.Banco,
                            Cuenta = det.Cuenta,
                            Imagen = imagenDestino
                        };
                        _context.IngresosEgresosDetalles.Add(detalleDestino);
                    }

                    await _context.SaveChangesAsync();

                    // Actualizar saldos de caja para el cajero destino
                    await ActualizarSaldosCajaAsync(cabecera.IdCajeroDestino.Value, idAlmacenDestino.Value, dto.Fecha);
                }
            }

            // Actualizar saldos de caja para la fecha del egreso/ingreso
            await ActualizarSaldosCajaAsync(dto.IdUsuario, dto.IdAlmacen, dto.Fecha);

            // Registrar también como Orden de Compra si es un egreso de pago a proveedores
            if (cabecera.Naturaleza == "E" &&
                cabecera.Tipo.Equals("PAGO PROVEEDORES", StringComparison.OrdinalIgnoreCase) &&
                cabecera.IdProveedor.HasValue)
            {
                var proveedor = await _context.Proveedors
                    .FirstOrDefaultAsync(p => p.IdProveedor == cabecera.IdProveedor.Value);
                if (proveedor != null)
                {
                    var formaPago = dto.Detalles.FirstOrDefault()?.Forma ?? "EFECTIVO";
                    var oc = new OrdenDeCompraCabecera
                    {
                        IdProveedor = proveedor.IdProveedor,
                        NumeroOc = $"PP-{cabecera.IdIngresoEgreso}",
                        FechaOc = cabecera.Fecha,
                        Rucproveedor = proveedor.Ruc ?? string.Empty,
                        NombreProveedor = proveedor.Nombre,
                        DireccionProveedor = proveedor.Direccion ?? string.Empty,
                        Moneda = "PEN",
                        TipoCambio = 1m,
                        FormaPago = formaPago,
                        SinIgv = false,
                        FechaEntrega = cabecera.Fecha,
                        Atencion = proveedor.Nombre,
                        Glosa = cabecera.Glosa,
                        ImporteSubTotal = cabecera.Monto,
                        ImporteIgv = 0m,
                        ImporteTotal = cabecera.Monto,
                        EstadoEmision = true,
                        EstadoAtencion = "TO",
                        FechaAtencionTotal = cabecera.Fecha,
                        IdUsuarioRegistra = cabecera.IdUsuario,
                        FechaRegistra = DateTime.Now
                    };
                    _context.OrdenDeCompraCabeceras.Add(oc);
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { cabecera.IdIngresoEgreso });
        }

        // GET: api/IngresosEgresos/foto/5
        [HttpGet("foto/{id}")]
        public async Task<IActionResult> GetFoto(int id)
        {
            var imagen = await _context.IngresosEgresosDetalles
                .Where(d => d.IdIngresoEgreso == id && d.Imagen != null)
                .Select(d => d.Imagen)
                .FirstOrDefaultAsync();

            if (imagen == null)
                return NotFound("No se encontró la foto");

            return File(imagen, "image/jpeg");
        }

        // PUT: api/IngresosEgresos/{id}/anular
        [HttpPut("{id}/anular")]
        public async Task<IActionResult> Anular(int id)
        {
            var registro = await _context.IngresosEgresosCabeceras.FindAsync(id);
            if (registro == null)
                return NotFound();

            if (registro.Estado == "A")
                return BadRequest("Registro ya anulado");

            registro.Estado = "A";
            await _context.SaveChangesAsync();

            // Si es un Egreso tipo PAGO PROVEEDORES, anular también la orden de compra asociada
            if (registro.Naturaleza == "E" && 
                registro.Tipo.Equals("PAGO PROVEEDORES", StringComparison.OrdinalIgnoreCase))
            {
                // Buscar la orden de compra asociada (formato PP-{IdIngresoEgreso})
                var numeroOc = $"PP-{registro.IdIngresoEgreso}";
                var ordenCompra = await _context.OrdenDeCompraCabeceras
                    .FirstOrDefaultAsync(oc => oc.NumeroOc == numeroOc);
                
                if (ordenCompra != null && ordenCompra.EstadoAtencion != "AN")
                {
                    ordenCompra.EstadoAtencion = "AN";
                    ordenCompra.FechaAnulado = DateTime.Now;
                    ordenCompra.FechaAtencionTotal = null;
                    await _context.SaveChangesAsync();
                }
            }
            
            // Si es un Egreso tipo TRANSFERENCIA, buscar y anular el Ingreso correspondiente del cajero destino
            if (registro.Naturaleza == "E" && 
                registro.Tipo.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase) &&
                registro.IdCajeroDestino.HasValue)
            {
                // Buscar el ingreso de transferencia del cajero destino que corresponda
                var ingresoDestino = await _context.IngresosEgresosCabeceras
                    .Where(i => i.Naturaleza == "I" &&
                                i.Tipo.Equals("TRANSFERENCIA", StringComparison.OrdinalIgnoreCase) &&
                                i.IdUsuario == registro.IdCajeroDestino.Value &&
                                i.IdAlmacenDestino == registro.IdAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == DateOnly.FromDateTime(registro.Fecha) &&
                                i.Monto == registro.Monto &&
                                i.Estado == "E")
                    .OrderByDescending(i => i.IdIngresoEgreso)
                    .FirstOrDefaultAsync();

                if (ingresoDestino != null)
                {
                    ingresoDestino.Estado = "A";
                    await _context.SaveChangesAsync();
                    
                    // Actualizar saldos de caja para el cajero destino
                    await ActualizarSaldosCajaAsync(ingresoDestino.IdUsuario, ingresoDestino.IdAlmacen, ingresoDestino.Fecha);
                }
            }

            // Actualizar saldos de caja para la fecha del egreso/ingreso anulado
            await ActualizarSaldosCajaAsync(registro.IdUsuario, registro.IdAlmacen, registro.Fecha);

            return NoContent();
        }

        /// <summary>
        /// Actualiza los saldos de caja para una fecha específica y recalcula todos los saldos posteriores
        /// </summary>
        private async Task ActualizarSaldosCajaAsync(int idUsuario, int idAlmacen, DateTime fecha)
        {
            var fechaDate = DateOnly.FromDateTime(fecha);
            
            // Buscar la apertura de caja para la fecha específica
            var apertura = await _context.AperturaCierreCajas
                .FirstOrDefaultAsync(a => a.IdUsuario == idUsuario &&
                                          a.IdAlmacen == idAlmacen &&
                                          a.Fecha == fechaDate);

            if (apertura != null)
            {
                // Recalcular ingresos y egresos para esta fecha
                var ingresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == idUsuario &&
                                i.IdAlmacen == idAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fechaDate &&
                                i.Naturaleza == "I" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                var egresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == idUsuario &&
                                i.IdAlmacen == idAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fechaDate &&
                                i.Naturaleza == "E" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                // Actualizar los valores de la caja
                apertura.Ingresos = ingresos;
                apertura.Egresos = egresos;
                apertura.SaldoFinal = apertura.SaldoInicial + apertura.VentaDia + ingresos - egresos;

                // Recalcular todos los saldos posteriores
                await RecalcularSaldosPosterioresAsync(apertura);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Si no existe una caja para esta fecha, pero se registró un ingreso/egreso,
                // necesitamos recalcular los saldos de las cajas posteriores
                await RecalcularSaldosDesdeFechaAsync(idUsuario, idAlmacen, fechaDate);
            }
        }

        /// <summary>
        /// Recalcula los saldos de todas las cajas posteriores a una fecha específica
        /// </summary>
        private async Task RecalcularSaldosDesdeFechaAsync(int idUsuario, int idAlmacen, DateOnly fecha)
        {
            // Buscar la última caja cerrada antes de la fecha
            var ultimaCajaAnterior = await _context.AperturaCierreCajas
                .Where(c => c.IdUsuario == idUsuario &&
                            c.IdAlmacen == idAlmacen &&
                            c.Fecha < fecha)
                .OrderByDescending(c => c.Fecha)
                .FirstOrDefaultAsync();

            if (ultimaCajaAnterior != null)
            {
                // Recalcular todos los saldos posteriores a partir de la última caja anterior
                await RecalcularSaldosPosterioresAsync(ultimaCajaAnterior);
                await _context.SaveChangesAsync();
            }
        }

        private async Task RecalcularSaldosPosterioresAsync(AperturaCierreCaja apertura)
        {
            var posteriores = await _context.AperturaCierreCajas
                .Where(c => c.IdUsuario == apertura.IdUsuario &&
                            c.IdAlmacen == apertura.IdAlmacen &&
                            c.Fecha > apertura.Fecha)
                .OrderBy(c => c.Fecha)
                .ToListAsync();

            var saldoAnterior = apertura.SaldoFinal;

            foreach (var caja in posteriores)
            {
                var saldoCalculado = saldoAnterior + caja.VentaDia + caja.Ingresos - caja.Egresos;

                if (caja.SaldoInicial != saldoAnterior || caja.SaldoFinal != saldoCalculado)
                {
                    caja.SaldoInicial = saldoAnterior;
                    caja.SaldoFinal = saldoCalculado;
                }

                saldoAnterior = caja.SaldoFinal;
            }
        }
    }
}