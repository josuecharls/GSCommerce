using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedorController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public ProveedorController(SyscharlesContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProveedorDTO>>> GetProveedors()
        {
            var proveedores = await _context.Proveedors
                .Select(p => new ProveedorDTO
                {
                    IdProveedor = p.IdProveedor,
                    NombreComercial = p.NombreComercial,
                    Nombre = p.Nombre,
                    TipoDocumento = p.TipoDocumento,
                    Ruc = p.Ruc,
                    TipoPersona = p.TipoPersona,
                    Dpd = p.Dpd,
                    Pais = p.Pais,
                    FormaPago = p.FormaPago,
                    Banco = p.Banco,
                    Cuenta = p.Cuenta,
                    Cci = p.Cci,
                    Contacto = p.Contacto,
                    Direccion = p.Direccion,
                    Telefono = p.Telefono,
                    Celular = p.Celular,
                    Estado = p.Estado,

                })
                .ToListAsync();
            return Ok(proveedores);
        }

        // GET: api/proveedor (Obtener todos los proveedores con paginación y búsqueda)
        [HttpGet("list")]
        public async Task<IActionResult> GetProveedoresList(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string? search = null)
        {
            var query = _context.Proveedors.AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Nombre.Contains(search) || p.Ruc.Contains(search));
            }
            var totalItems = await query.CountAsync();
            var proveedorList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return Ok(new
            {
                TotalItems = totalItems,
                TotalPages = (int)System.Math.Ceiling((double)totalItems / pageSize),
                Data = proveedorList
            });
        }


        // GET: api/proveedor/5 (Obtener un proveedor por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Proveedor>> GetProveedor(int id)
        {
            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return proveedor;
        }

        // POST: api/proveedor (Crear un nuevo proveedor)
        [HttpPost]
        public async Task<ActionResult<Proveedor>> PostProveedor(Proveedor proveedor)
        {
            _context.Proveedors.Add(proveedor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProveedor), new { id = proveedor.IdProveedor }, proveedor);
        }

        // PUT: api/proveedor/5 (Actualizar un proveedor)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProveedor(int id, Proveedor proveedor)
        {
            if (id != proveedor.IdProveedor)
            {
                return BadRequest();
            }
            _context.Entry(proveedor).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedorExists(id))
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

        // DELETE: api/proveedor/5 (Eliminar un proveedor)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProveedor(int id)
        {
            var proveedor = await _context.Proveedors.FindAsync(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            _context.Proveedors.Remove(proveedor);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Método privado para verificar si un proveedor existe
        private bool ProveedorExists(int id)
        {
            return _context.Proveedors.Any(e => e.IdProveedor == id);
        }
    }
}
