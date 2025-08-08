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
                query = query.Where(q => q.Naturaleza == naturaleza);

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
                Naturaleza = dto.Naturaleza,
                Tipo = dto.Tipo,
                Fecha = dto.Fecha,
                Glosa = dto.Glosa,
                Monto = dto.Monto,
                IdProveedor = dto.IdProveedor,
                IdAlmacenDestino = dto.IdAlmacenDestino,
                IdCajeroDestino = dto.IdCajeroDestino,
                FechaRegistro = DateTime.Now,
                Estado = "A"
            };

            _context.IngresosEgresosCabeceras.Add(cabecera);
            await _context.SaveChangesAsync();

            foreach (var det in dto.Detalles)
            {
                var detalle = new IngresosEgresosDetalle
                {
                    IdIngresoEgreso = cabecera.IdIngresoEgreso,
                    Forma = det.Forma,
                    Detalle = det.Detalle,
                    Monto = det.Monto,
                    Banco = det.Banco,
                    Cuenta = det.Cuenta,
                    Imagen = string.IsNullOrEmpty(det.ImagenBase64) ? null : Convert.FromBase64String(det.ImagenBase64)
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
                if (dto.Naturaleza.Equals("Ingreso", StringComparison.OrdinalIgnoreCase))
                    apertura.Ingresos += dto.Monto;
                else
                    apertura.Egresos += dto.Monto;

                apertura.SaldoFinal = apertura.SaldoInicial + apertura.VentaDia + apertura.Ingresos - apertura.Egresos;
                await _context.SaveChangesAsync();
            }

            return Ok(new { cabecera.IdIngresoEgreso });
        }
    }
}