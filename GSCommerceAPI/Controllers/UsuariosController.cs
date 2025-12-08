using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
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
        private readonly PasswordHasherService _passwordHasher;

        public UsuariosController(SyscharlesContext context, PasswordHasherService passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
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
        [HttpGet("administradores")]
        public async Task<IActionResult> GetAdministradores()
        {
            var administradores = await _context.Personals
                .Where(p => p.Cargo == "ADMINISTRADOR" || p.Cargo == "CONTADOR" && p.Estado == true)
                .Select(p => new
                {
                    p.IdPersonal,
                    Nombre = p.Nombres + " " + p.Apellidos,
                    Usuario = _context.Usuarios.FirstOrDefault(u => u.IdPersonal == p.IdPersonal)
                })
                .ToListAsync();

            var resultado = administradores.Select(a => new
            {
                IdUsuario = a.Usuario != null ? a.Usuario.IdUsuario : 0,
                a.Nombre,
                a.IdPersonal,
                Estado = a.Usuario?.Estado ?? false,
                Clave = (string?)null
            });

            return Ok(resultado);
        }

        [HttpPost("cambiar-password")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordRequest request)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Nombre == request.NombreUsuario);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            var passwordActual = await GetPasswordFromDB(request.NombreUsuario);
            if (passwordActual == null || !_passwordHasher.VerifyPassword(request.PasswordActual, passwordActual))
                return Unauthorized("Contraseña actual incorrecta.");

            // Hashear la nueva contraseña antes de guardarla
            var hashedPassword = _passwordHasher.HashPassword(request.PasswordNuevo);

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("dbo.usp_set_password", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = usuario.IdUsuario;
                command.Parameters.Add("@password", SqlDbType.VarChar, 200).Value = hashedPassword; // Aumentar tamaño para hash
                await command.ExecuteNonQueryAsync();
            }

            return Ok();
        }

        private async Task<string?> GetPasswordFromDB(string usuario)
        {
            string? password = null;
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                try
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("dbo.usp_get_password", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@usuario", usuario);
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            password = result.ToString();
                            // Si el resultado es una cadena vacía, tratarlo como null
                            if (string.IsNullOrWhiteSpace(password))
                                password = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log del error para debugging
                    Console.WriteLine($"Error al obtener contraseña para usuario {usuario}: {ex.Message}");
                    throw; // Re-lanzar para que el método de migración lo capture
                }
            }
            return password;
        }

        private async Task SetPasswordHash(int idUsuario, string hashedPassword)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("dbo.usp_set_password", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
                command.Parameters.Add("@password", SqlDbType.VarChar, 200).Value = hashedPassword;
                await command.ExecuteNonQueryAsync();
            }
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
                .Select(p => new
                {
                    p.IdPersonal,
                    Nombre = p.Nombres + " " + p.Apellidos,
                    Usuario = _context.Usuarios.FirstOrDefault(u => u.IdPersonal == p.IdPersonal)
                })
                .ToListAsync();

            var resultado = cajeros.Select(c => new UsuarioDTO
            {
                IdUsuario = c.Usuario != null ? c.Usuario.IdUsuario : 0,
                Nombre = c.Nombre,
                IdPersonal = c.IdPersonal,
                Estado = c.Usuario != null ? c.Usuario.Estado : false
            }).Where(u => u.IdUsuario > 0).ToList(); // Solo devolver cajeros que tengan usuario

            return Ok(resultado);
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

            // Hashear la contraseña antes de guardarla
            var hashedPassword = _passwordHasher.HashPassword(request.Password);

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("dbo.usp_set_password", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = usuario.IdUsuario;
                command.Parameters.Add("@password", SqlDbType.VarChar, 200).Value = hashedPassword; // Aumentar tamaño para hash
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

        [HttpPost("migrar-contrasenas-bcrypt")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public async Task<IActionResult> MigrarContrasenasABcrypt()
        {
            try
            {
                var usuarios = await _context.Usuarios.Where(u => u.Estado == true).ToListAsync();
                var migrados = 0;
                var yaMigrados = 0;
                var errores = 0;
                var erroresDetalle = new List<string>();

                foreach (var usuario in usuarios)
                {
                    try
                    {
                        var passwordActual = await GetPasswordFromDB(usuario.Nombre);

                        if (string.IsNullOrEmpty(passwordActual))
                        {
                            errores++;
                            erroresDetalle.Add($"Usuario {usuario.Nombre}: No se pudo obtener contraseña");
                            continue;
                        }

                        // Verificar si ya es un hash BCrypt (empieza con $2a$, $2b$, $2y$)
                        if (passwordActual.StartsWith("$2"))
                        {
                            yaMigrados++;
                            continue;
                        }

                        // Hashear la contraseña con BCrypt
                        var hashedPassword = _passwordHasher.HashPassword(passwordActual);

                        // Guardar el hash
                        await SetPasswordHash(usuario.IdUsuario, hashedPassword);
                        migrados++;
                    }
                    catch (Exception ex)
                    {
                        errores++;
                        erroresDetalle.Add($"Usuario {usuario.Nombre}: {ex.Message}");
                    }
                }

                return Ok(new
                {
                    mensaje = "Migración completada",
                    totalUsuarios = usuarios.Count,
                    migrados = migrados,
                    yaMigrados = yaMigrados,
                    errores = errores,
                    erroresDetalle = erroresDetalle
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error en la migración: {ex.Message}" });
            }
        }
    }
}