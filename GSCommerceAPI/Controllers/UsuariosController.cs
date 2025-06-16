using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly SyscharlesContext _context;

        public UsuariosController(SyscharlesContext context)
        {
            _context = context;
        }

        // En UsuariosController.cs
        [HttpGet("info/{idUsuario}")]
        public async Task<IActionResult> ObtenerInfoPersonal(int idUsuario)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.IdPersonalNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

            if (usuario == null || usuario.IdPersonalNavigation == null)
                return NotFound("Usuario o personal no encontrado.");

            var nombreCompleto = $"{usuario.IdPersonalNavigation.Nombres} {usuario.IdPersonalNavigation.Apellidos}";
            return Ok(nombreCompleto);
        }

        // GET: api/usuarios (Obtener todos los usuarios)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // GET: api/usuarios/5 (Obtener un usuario por ID)
        [HttpGet("{id}")]
        public async Task<ActionResult<Usuario>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }
            return usuario;
        }

        // POST: api/usuarios (Crear un nuevo usuario)
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuario), new { id = usuario.IdUsuario }, usuario);
        }

        // PUT: api/usuarios/5 (Actualizar un usuario)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return BadRequest();
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
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

        [HttpGet("cajeros/{idAlmacen}")]
        public async Task<ActionResult<IEnumerable<UsuarioDTO>>> ObtenerCajerosPorAlmacen(int idAlmacen)
        {
            var cajeros = await _context.Personals
                .Where(p => p.Cargo == "CAJERO" && p.IdAlmacen == idAlmacen && p.Estado == true)
                .Select(p => new UsuarioDTO
                {
                    IdPersonal = p.IdPersonal,
                    Nombre = $"{p.Nombres} {p.Apellidos}"   
                })
                .ToListAsync();

            return Ok(cajeros);
        }

        [HttpPost("generar-credenciales")]
        public async Task<IActionResult> GenerarCredenciales([FromBody] GenerarCredencialesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreUsuario) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Usuario y contraseña requeridos.");

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdPersonal == request.IdPersonal);
            if (usuario == null)
            {
                usuario = new Usuario
                {
                    Nombre = request.NombreUsuario,
                    IdPersonal = request.IdPersonal,
                    Estado = true
                };
                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();
            }
            else
            {
                usuario.Nombre = request.NombreUsuario;
                _context.Entry(usuario).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("dbo.usp_set_password", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = usuario.IdUsuario;
                command.Parameters.Add("@password", SqlDbType.VarChar, 50).Value = request.Password;
                await command.ExecuteNonQueryAsync();
            }

            return Ok(new { usuario.IdUsuario, usuario.Nombre });
        }

        [HttpGet("cajeros2/{idAlmacen}")]
        public async Task<IActionResult> GetCajerosPorAlmacen(int idAlmacen)
        {
            var cajeros = await _context.Personals
                .Where(p => p.Cargo == "CAJERO" && p.IdAlmacen == idAlmacen && p.Estado == true)
                .Select(p => new
                {
                    p.IdPersonal,
                    Nombre = p.Nombres + " " + p.Apellidos,
                    Usuario = _context.Usuarios.FirstOrDefault(u => u.IdPersonal == p.IdPersonal)
                })
                .ToListAsync();

            var resultado = cajeros.Select(c => new
            {
                IdUsuario = c.Usuario != null ? c.Usuario.IdUsuario : 0,
                c.Nombre,
                c.IdPersonal,
                Estado = c.Usuario?.Estado ?? false,
                Clave = (string?)null // No se devuelve clave
            });

            return Ok(resultado);
        }


        // DELETE: api/usuarios/5 (Eliminar un usuario)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}