using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public AlmacenController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/almacen?soloActivos=true
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AlmacenDTO>>> GetAlmacens([FromQuery] bool soloActivos = true)
        {
            var query = _context.Almacens.AsNoTracking().AsQueryable();
            if (soloActivos)
            {
                query = query.Where(a => a.Estado);
            }

            var almacenes = await query.OrderBy(a => a.Nombre).ToListAsync();
            var lista = new List<AlmacenDTO>();

            foreach (var a in almacenes)
            {
                var key = $"MonedaAlmacen_{a.IdAlmacen}";
                var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

                lista.Add(MapToDto(a, config?.Valor ?? "PEN"));
            }

            return Ok(lista);
        }

        // GET: api/almacen/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AlmacenDTO>> GetAlmacen(int id)
        {
            var almacen = await _context.Almacens.AsNoTracking().FirstOrDefaultAsync(a => a.IdAlmacen == id);
            if (almacen == null)
            {
                return NotFound();
            }

            var key = $"MonedaAlmacen_{almacen.IdAlmacen}";
            var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

            return Ok(MapToDto(almacen, config?.Valor ?? "PEN"));
        }

        [HttpGet("list")]
        public async Task<ActionResult> GetAlmacenList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _context.Almacens.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Nombre.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var almacenEntities = await query
                .OrderBy(a => a.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var almacenList = new List<AlmacenDTO>();

            foreach (var a in almacenEntities)
            {
                var key = $"MonedaAlmacen_{a.IdAlmacen}";
                var config = await _context.Configuracions.FirstOrDefaultAsync(c => c.Configuracion1 == key);

                almacenList.Add(MapToDto(a, config?.Valor ?? "PEN"));
            }

            var response = new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = almacenList
            };

            return Ok(response);
        }

        // PUT: api/almacen/5
        [HttpPut("{id}")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> PutAlmacen(int id, AlmacenDTO dto)
        {
            if (id != dto.IdAlmacen)
            {
                return BadRequest();
            }

            var almacen = await _context.Almacens.FindAsync(id);
            if (almacen == null)
            {
                return NotFound();
            }

            almacen.Nombre = dto.Nombre.Trim();
            almacen.EsTienda = dto.EsTienda;
            almacen.Direccion = dto.Direccion.Trim();
            almacen.Dpd = string.IsNullOrWhiteSpace(dto.Dpd) ? null : dto.Dpd.Trim();
            almacen.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            almacen.Celular = string.IsNullOrWhiteSpace(dto.Celular) ? null : dto.Celular.Trim();
            almacen.RazonSocial = string.IsNullOrWhiteSpace(dto.RazonSocial) ? null : dto.RazonSocial.Trim();
            almacen.Ruc = string.IsNullOrWhiteSpace(dto.Ruc) ? null : dto.Ruc.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Ubigeo))
            {
                almacen.Ubigeo = dto.Ubigeo.Trim();
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/almacen/5/estado
        [HttpPatch("{id}/estado")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> SetAlmacenEstado(int id, [FromBody] AlmacenEstadoDTO request)
        {
            var almacen = await _context.Almacens.FindAsync(id);
            if (almacen == null)
            {
                return NotFound();
            }

            almacen.Estado = request.Estado;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static AlmacenDTO MapToDto(Almacen a, string moneda) => new()
        {
            IdAlmacen = a.IdAlmacen,
            Nombre = a.Nombre,
            EsTienda = a.EsTienda,
            Direccion = a.Direccion,
            Dpd = a.Dpd,
            Telefono = a.Telefono,
            Celular = a.Celular,
            RazonSocial = a.RazonSocial,
            Ruc = a.Ruc,
            Estado = a.Estado,
            Ubigeo = a.Ubigeo,
            Moneda = moneda
        };

        private bool AlmacenExists(int id)
        {
            return _context.Almacens.Any(e => e.IdAlmacen == id);
        }
    }
}
