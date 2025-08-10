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

        // GET: api/IngresosEgresos?idAlmacen=1&fechaInicio=2024-01-01&fechaFin=2024-01-31&naturaleza=Egreso
        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int? idAlmacen, [FromQuery] int? idUsuario,
            [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin, [FromQuery] string? naturaleza)
        {
            var query = _context.VListadoIngresosEgresos1s.AsQueryable();

            if (idAlmacen.HasValue)
                query = query.Where(q => q.IdAlmacen == idAlmacen);

            if (idUsuario.HasValue)
                query = query.Where(q => q.IdUsuario == idUsuario);

            if (fechaInicio.HasValue)
                query = query.Where(q => q.Fecha >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(q => q.Fecha <= fechaFin.Value);

            if (!string.IsNullOrEmpty(naturaleza))
            {
                var nat = naturaleza.StartsWith("I", StringComparison.OrdinalIgnoreCase) ? "I" : "E";
                query = query.Where(q => q.Naturaleza == nat);
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

            return Ok(new { cabecera.IdIngresoEgreso });
        }
    }
}