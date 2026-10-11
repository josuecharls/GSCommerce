using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GSCommerce.Client.Models.SUNAT;
using GSCommerceAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ComprobantesController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public ComprobantesController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? desde = null,
            [FromQuery] string? hasta = null,
            [FromQuery] int? idAlmacen = null,
            [FromQuery] string? estado = null,
            [FromQuery(Name = "tipoDoc")] int? idTipoDoc = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 200) pageSize = 200;

            // Fechas como calendario local (sin UTC): evita que "01/08" arrastre el 31/07
            var dDesde = ParseFechaCalendario(desde);
            var dHasta = ParseFechaCalendario(hasta);

            var query = from f in _context.Comprobantes.AsNoTracking()
                        join c in _context.ComprobanteDeVentaCabeceras.AsNoTracking() on f.IdComprobante equals c.IdComprobante
                        join a in _context.Almacens.AsNoTracking() on c.IdAlmacen equals a.IdAlmacen
                        join t in _context.TipoDocumentoVenta.AsNoTracking() on c.IdTipoDocumento equals t.IdTipoDocumentoVenta
                        select new
                        {
                            f.IdFe,
                            Tienda = a.Nombre,
                            a.IdAlmacen,
                            TipoDoc = t.Descripcion,
                            IdTipoDoc = t.IdTipoDocumentoVenta,
                            c.Serie,
                            c.Numero,
                            c.Apagar,
                            c.Fecha,
                            f.Hash,
                            f.EnviadoSunat,
                            f.Estado,
                            f.FechaEnvio,
                            f.FechaRespuestaSunat,
                            f.RespuestaSunat,
                            f.TicketSunat,
                            f.Xml,
                            f.IdComprobante
                        };

            if (dDesde.HasValue)
            {
                var desdeInicio = dDesde.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                query = query.Where(x => x.FechaEnvio != null && x.FechaEnvio >= desdeInicio);
            }

            if (dHasta.HasValue)
            {
                var hastaExclusivo = dHasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
                query = query.Where(x => x.FechaEnvio != null && x.FechaEnvio < hastaExclusivo);
            }

            if (idAlmacen.HasValue)
                query = query.Where(x => x.IdAlmacen == idAlmacen.Value);
            if (idTipoDoc.HasValue)
                query = query.Where(x => x.IdTipoDoc == idTipoDoc.Value);
            if (!string.IsNullOrWhiteSpace(estado))
            {
                var e = estado.ToLower();
                if (e == "enviado")
                    query = query.Where(x => x.EnviadoSunat == true && x.Estado);
                else if (e == "pendiente")
                    query = query.Where(x => x.EnviadoSunat != true && (x.RespuestaSunat == null || x.RespuestaSunat == ""));
                else if (e == "rechazado")
                    query = query.Where(x => (x.EnviadoSunat == true && !x.Estado) || (x.EnviadoSunat != true && x.RespuestaSunat != null && x.RespuestaSunat != ""));
            }

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            var rows = await query
                .OrderByDescending(x => x.FechaEnvio)
                .ThenBy(x => x.Numero)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var data = rows.Select(x => new PendienteSunatDTO
            {
                IdFe = x.IdFe,
                Tienda = x.Tienda,
                IdAlmacen = x.IdAlmacen,
                TipoDoc = x.TipoDoc,
                Numero = $"{x.Serie}-{x.Numero:D8}",
                Fecha = x.Fecha.ToString("dd/MM/yyyy"),
                Apagar = x.Apagar,
                Hash = x.Hash,
                EnviadoSunat = x.EnviadoSunat ?? false,
                FechaEnvio = x.FechaEnvio,
                FechaRespuestaSunat = x.FechaRespuestaSunat,
                RespuestaSunat = x.RespuestaSunat,
                TicketSunat = x.TicketSunat,
                Xml = x.Xml,
                IdComprobante = x.IdComprobante,
                EstadoSunat = x.EnviadoSunat == true
                    ? (x.Estado ? "Enviado" : "Rechazado")
                    : (string.IsNullOrWhiteSpace(x.RespuestaSunat) ? "Pendiente" : "Rechazado")
            }).ToList();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                Page = page,
                PageSize = pageSize,
                Data = data
            });
        }

        /// <summary>
        /// Interpreta yyyy-MM-dd (o fecha ISO) como día de calendario, sin convertir a UTC.
        /// </summary>
        private static DateOnly? ParseFechaCalendario(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var s = value.Trim();
            if (s.Length >= 10)
                s = s[..10];

            if (DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;

            if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d;

            return null;
        }
    }
}
