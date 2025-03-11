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

        // GET: api/clientes (Obtener todos los clientes)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes()
        {
            return await _context.Clientes.ToListAsync();
        }
        // GET: api/clientes/5 (Obtener un cliente por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Cliente>> GetCliente(int id) // 👈 Cambiar string por int
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return cliente;
        }

        // PUT: api/clientes/5 (Actualizar un cliente)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente) // Cambiar string por int
        {
            if (id != cliente.IdCliente) // Ahora ambos son int
            {
                return BadRequest();
            }

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ClientesExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/clientes/5 (Eliminar un cliente)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id) // Cambiar string por int
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound();
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ClientesExists(int id) // Cambiar string por int
        {
            return _context.Clientes.Any(e => e.IdCliente == id);
        }
    }
}