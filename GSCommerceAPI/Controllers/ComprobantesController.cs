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
        // Tipos que no se declaran ante SUNAT / no deben listarse aquí
        private static readonly int[] TiposExcluidos =
        {
            4,  // TICKET
            10, // NOTA CREDITO M
            11  // NOTA CREDITO T
        };

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

            // Ventas (boleta/factura/etc.) — FE.EsNota = false
            var ventasQuery =
                from f in _context.Comprobantes.AsNoTracking()
                where !f.EsNota
                join c in _context.ComprobanteDeVentaCabeceras.AsNoTracking()
                    on f.IdComprobante equals c.IdComprobante
                where !TiposExcluidos.Contains(c.IdTipoDocumento)
                join a in _context.Almacens.AsNoTracking() on c.IdAlmacen equals a.IdAlmacen
                join t in _context.TipoDocumentoVenta.AsNoTracking()
                    on c.IdTipoDocumento equals t.IdTipoDocumentoVenta
                select new ComprobanteSunatRow
                {
                    IdFe = f.IdFe,
                    Tienda = a.Nombre,
                    IdAlmacen = a.IdAlmacen,
                    TipoDoc = t.Descripcion,
                    IdTipoDoc = t.IdTipoDocumentoVenta,
                    Serie = c.Serie,
                    Numero = c.Numero,
                    Apagar = c.Apagar,
                    Fecha = c.Fecha,
                    Hash = f.Hash,
                    EnviadoSunat = f.EnviadoSunat,
                    Estado = f.Estado,
                    FechaEnvio = f.FechaEnvio,
                    FechaRespuestaSunat = f.FechaRespuestaSunat,
                    RespuestaSunat = f.RespuestaSunat,
                    TicketSunat = f.TicketSunat,
                    Xml = f.Xml,
                    IdComprobante = f.IdComprobante
                };

            // Notas de crédito — FE.EsNota = true (IdComprobante = IdNc)
            var notasQuery =
                from f in _context.Comprobantes.AsNoTracking()
                where f.EsNota
                join n in _context.NotaDeCreditoCabeceras.AsNoTracking()
                    on f.IdComprobante equals n.IdNc
                where !TiposExcluidos.Contains(n.IdTipoDocumento)
                join a in _context.Almacens.AsNoTracking() on n.IdAlmacen equals a.IdAlmacen
                join t in _context.TipoDocumentoVenta.AsNoTracking()
                    on n.IdTipoDocumento equals t.IdTipoDocumentoVenta
                select new ComprobanteSunatRow
                {
                    IdFe = f.IdFe,
                    Tienda = a.Nombre,
                    IdAlmacen = a.IdAlmacen,
                    TipoDoc = t.Descripcion,
                    IdTipoDoc = t.IdTipoDocumentoVenta,
                    Serie = n.Serie,
                    Numero = n.Numero,
                    Apagar = n.Total,
                    Fecha = n.Fecha,
                    Hash = f.Hash,
                    EnviadoSunat = f.EnviadoSunat,
                    Estado = f.Estado,
                    FechaEnvio = f.FechaEnvio,
                    FechaRespuestaSunat = f.FechaRespuestaSunat,
                    RespuestaSunat = f.RespuestaSunat,
                    TicketSunat = f.TicketSunat,
                    Xml = f.Xml,
                    IdComprobante = f.IdComprobante
                };

            var query = ventasQuery.Concat(notasQuery);

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

        private sealed class ComprobanteSunatRow
        {
            public int IdFe { get; set; }
            public string Tienda { get; set; } = string.Empty;
            public int IdAlmacen { get; set; }
            public string TipoDoc { get; set; } = string.Empty;
            public int IdTipoDoc { get; set; }
            public string Serie { get; set; } = string.Empty;
            public int Numero { get; set; }
            public decimal? Apagar { get; set; }
            public DateTime Fecha { get; set; }
            public string Hash { get; set; } = string.Empty;
            public bool? EnviadoSunat { get; set; }
            public bool Estado { get; set; }
            public DateTime? FechaEnvio { get; set; }
            public DateTime? FechaRespuestaSunat { get; set; }
            public string? RespuestaSunat { get; set; }
            public string? TicketSunat { get; set; }
            public string? Xml { get; set; }
            public int IdComprobante { get; set; }
        }
    }
}
