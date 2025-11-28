using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace GSCommerceAPI.Controllers
{
    [ApiController]
    [Route("api/notificaciones")]
    [Authorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public NotificacionesController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/notificaciones
        [HttpGet]
        public async Task<IActionResult> GetNotificaciones([FromQuery] bool soloNoLeidas = true)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var query = _context.Notificaciones
                .Include(n => n.IdMovimientoNavigation)
                .Where(n => n.IdUsuario == userId);

            if (soloNoLeidas)
                query = query.Where(n => !n.Leida);

            // Filtrar notificaciones de transferencias que ya están confirmadas
            // Solo mostrar notificaciones de guías que aún están en estado "E" (Emitido)
            query = query.Where(n => 
                n.IdMovimientoNavigation.Estado == "E" || 
                n.IdMovimientoNavigation.Estado == "A" || 
                n.Tipo != "TRANSFERENCIA");

            var notificaciones = await query
                .OrderByDescending(n => n.FechaCreacion)
                .Select(n => new
                {
                    n.IdNotificacion,
                    n.IdMovimiento,
                    n.Tipo,
                    n.Mensaje,
                    n.Leida,
                    n.FechaCreacion,
                    NumeroGuia = n.IdMovimientoNavigation.IdMovimiento,
                    EstadoGuia = n.IdMovimientoNavigation.Estado
                })
                .ToListAsync();

            return Ok(notificaciones);
        }

        // GET: api/notificaciones/count
        [HttpGet("count")]
        public async Task<IActionResult> GetCountNotificacionesNoLeidas()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var count = await _context.Notificaciones
                .CountAsync(n => n.IdUsuario == userId && !n.Leida);

            return Ok(new { count });
        }

        // PUT: api/notificaciones/{id}/marcar-leida
        [HttpPut("{id}/marcar-leida")]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var notificacion = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.IdNotificacion == id && n.IdUsuario == userId);

            if (notificacion == null)
                return NotFound();

            notificacion.Leida = true;
            notificacion.FechaLectura = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Notificación marcada como leída" });
        }

        // PUT: api/notificaciones/marcar-todas-leidas
        [HttpPut("marcar-todas-leidas")]
        public async Task<IActionResult> MarcarTodasComoLeidas()
        {
            var userIdClaim = User.FindFirst("userId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var notificaciones = await _context.Notificaciones
                .Where(n => n.IdUsuario == userId && !n.Leida)
                .ToListAsync();

            foreach (var notificacion in notificaciones)
            {
                notificacion.Leida = true;
                notificacion.FechaLectura = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { mensaje = "Todas las notificaciones marcadas como leídas" });
        }
    }
}


