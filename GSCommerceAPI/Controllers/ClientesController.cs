using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public ClientesController(SyscharlesContext context)
        {
            _context = context;
        }

        // GET: api/clientes?page=1&pageSize=10&search=texto
        [HttpGet]
        public async Task<IActionResult> GetClientes(int page = 1, int pageSize = 10, string? search = null)
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Nombre.Contains(search) ||
                    c.Dniruc.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var clientes = await query
                .OrderBy(c => c.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClienteDTO
                {
                    IdCliente = c.IdCliente,
                    TipoDocumento = c.TipoDocumento,
                    Dniruc = c.Dniruc,
                    Nombre = c.Nombre,
                    Direccion = c.Direccion,
                    Dpd = c.Dpd,
                    Telefono = c.Telefono,
                    Celular = c.Celular,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = clientes
            });
        }

        // GET: api/clientes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClienteDTO>> GetCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            return new ClienteDTO
            {
                IdCliente = cliente.IdCliente,
                TipoDocumento = cliente.TipoDocumento,
                Dniruc = cliente.Dniruc,
                Nombre = cliente.Nombre,
                Direccion = cliente.Direccion,
                Dpd = cliente.Dpd,
                Telefono = cliente.Telefono,
                Celular = cliente.Celular,
                Email = cliente.Email,
                Estado = cliente.Estado
            };
        }

        // POST: api/clientes
        [HttpPost]
        public async Task<IActionResult> CreateCliente([FromBody] ClienteDTO dto)
        {
            var cliente = new Cliente
            {
                TipoDocumento = dto.TipoDocumento,
                Dniruc = dto.Dniruc,
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Dpd = dto.Dpd,
                Telefono = dto.Telefono,
                Celular = dto.Celular,
                Email = dto.Email,
                Estado = dto.Estado
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            dto.IdCliente = cliente.IdCliente;

            return Ok(dto);
        }

        // PUT: api/clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] ClienteDTO dto)
        {
            if (id != dto.IdCliente) return BadRequest("ID no coincide");

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.TipoDocumento = dto.TipoDocumento;
            cliente.Dniruc = dto.Dniruc;
            cliente.Nombre = dto.Nombre;
            cliente.Direccion = dto.Direccion;
            cliente.Dpd = dto.Dpd;
            cliente.Telefono = dto.Telefono;
            cliente.Celular = dto.Celular;
            cliente.Email = dto.Email;
            cliente.Estado = dto.Estado;

            await _context.SaveChangesAsync();
            return Ok();
        }
        // GET: api/clientes/list?page=1&pageSize=10&search=texto
        [HttpGet("list")]
        public async Task<ActionResult> GetClientesList(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? search = null)
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Nombre.Contains(search) || c.Dniruc.Contains(search));
            }

            var totalItems = await query.CountAsync();

            var clienteList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ClienteDTO
                {
                    IdCliente = c.IdCliente,
                    TipoDocumento = c.TipoDocumento,
                    Dniruc = c.Dniruc,
                    Nombre = c.Nombre,
                    Direccion = c.Direccion,
                    Dpd = c.Dpd,
                    Telefono = c.Telefono,
                    Celular = c.Celular,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .ToListAsync();

            var response = new
            {
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
                Data = clienteList
            };

            return Ok(response);
        }

        // DELETE: api/clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/clientes/documento/12345678
        [HttpGet("documento/{documento}")]
        public async Task<ActionResult<ClienteDTO>> GetClientePorDocumento(string documento)
        {
            var cliente = await _context.Clientes
                .Where(c => c.Dniruc == documento)
                .Select(c => new ClienteDTO
                {
                    IdCliente = c.IdCliente,
                    TipoDocumento = c.TipoDocumento,
                    Dniruc = c.Dniruc,
                    Nombre = c.Nombre,
                    Direccion = c.Direccion,
                    Dpd = c.Dpd,
                    Telefono = c.Telefono,
                    Celular = c.Celular,
                    Email = c.Email,
                    Estado = c.Estado
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }

    }
}