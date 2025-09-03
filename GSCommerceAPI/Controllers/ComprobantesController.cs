using System;
using System.Collections.Generic;
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
        public async Task<ActionResult<IEnumerable<PendienteSunatDTO>>> Get(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? idAlmacen = null,
            [FromQuery] string? estado = null,
            [FromQuery(Name = "tipoDoc")] int? idTipoDoc = null)
        {
            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
                return Forbid();

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

            if (desde.HasValue)
                query = query.Where(x => x.FechaEnvio >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(x => x.FechaEnvio < hasta.Value.AddDays(1));
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

            var rows = await query
                .OrderByDescending(x => x.FechaEnvio)
                .ThenBy(x => x.Numero)
                .ToListAsync();

            var result = rows.Select(x => new PendienteSunatDTO
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

            return Ok(result);
        }
    }
}