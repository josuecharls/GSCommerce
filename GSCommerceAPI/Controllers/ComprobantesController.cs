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
        public async Task<ActionResult<IEnumerable<PendienteSunatDTO>>> Get([FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var cargo = User.FindFirst("Cargo")?.Value ?? string.Empty;
            if (!string.Equals(cargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            var query = _context.VComprobantes.AsNoTracking().Where(c => c.EnviadoSunat);

            if (desde.HasValue)
                query = query.Where(c => c.FechaEnvio >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(c => c.FechaEnvio <= hasta.Value);

            var rows = await query
                .OrderByDescending(c => c.FechaEnvio)
                .ThenBy(c => c.Numero)
                .ToListAsync();

            var result = rows.Select(c => new PendienteSunatDTO
            {
                IdFe = c.IdFe,
                Tienda = c.Tienda,
                TipoDoc = c.TipoDoc,
                Numero = c.Numero,
                Fecha = c.Fecha,
                Apagar = c.Apagar,
                Hash = c.Hash,
                EnviadoSunat = c.EnviadoSunat,
                FechaEnvio = c.FechaEnvio,
                FechaRespuestaSunat = c.FechaRespuestaSunat,
                RespuestaSunat = c.RespuestaSunat,
                TicketSunat = c.TicketSunat,
                Xml = c.Xml,
                IdComprobante = c.IdComprobante
            }).ToList();

            return Ok(result);
        }
    }
}