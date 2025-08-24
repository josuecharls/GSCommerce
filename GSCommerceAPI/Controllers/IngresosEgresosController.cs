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

            var apertura = await _context.AperturaCierreCajas
                .FirstOrDefaultAsync(a => a.IdUsuario == dto.IdUsuario &&
                                          a.IdAlmacen == dto.IdAlmacen &&
                                          a.Fecha == DateOnly.FromDateTime(dto.Fecha));

            if (apertura != null)
            {
                var fecha = DateOnly.FromDateTime(dto.Fecha);

                var ingresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == dto.IdUsuario &&
                                i.IdAlmacen == dto.IdAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fecha &&
                                i.Naturaleza == "I" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                var egresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == dto.IdUsuario &&
                                i.IdAlmacen == dto.IdAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fecha &&
                                i.Naturaleza == "E" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                apertura.Ingresos = ingresos;
                apertura.Egresos = egresos;
                apertura.SaldoFinal = apertura.SaldoInicial + apertura.VentaDia + ingresos - egresos;
                await _context.SaveChangesAsync();
            }

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

            var fecha = DateOnly.FromDateTime(registro.Fecha);

            var apertura = await _context.AperturaCierreCajas
                .FirstOrDefaultAsync(a => a.IdUsuario == registro.IdUsuario &&
                                          a.IdAlmacen == registro.IdAlmacen &&
                                          a.Fecha == fecha);

            if (apertura != null)
            {
                var ingresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == registro.IdUsuario &&
                                i.IdAlmacen == registro.IdAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fecha &&
                                i.Naturaleza == "I" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                var egresos = await _context.IngresosEgresosCabeceras
                    .Where(i => i.IdUsuario == registro.IdUsuario &&
                                i.IdAlmacen == registro.IdAlmacen &&
                                DateOnly.FromDateTime(i.Fecha) == fecha &&
                                i.Naturaleza == "E" &&
                                i.Estado == "E")
                    .SumAsync(i => (decimal?)i.Monto) ?? 0;

                apertura.Ingresos = ingresos;
                apertura.Egresos = egresos;
                apertura.SaldoFinal = apertura.SaldoInicial + apertura.VentaDia + ingresos - egresos;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}