using GSCommerce.Client.Pages;
using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GSCommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SyscharlesContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(SyscharlesContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Usuario) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Usuario y contraseña requeridos.");
            }

            var storedPassword = await GetPasswordFromDB(request.Usuario);
            if (storedPassword == null || storedPassword != request.Password)
            {   
                return Unauthorized("Credenciales inválidas.");
            }

            var user = await _context.Usuarios
                .Include(u => u.IdPersonalNavigation) // Traemos la información del personal
                .FirstOrDefaultAsync(u => u.Nombre == request.Usuario);

            if (user == null || !user.Estado)
            {
                return Unauthorized("Usuario inactivo o no encontrado.");
            }

            string cargo = user.IdPersonalNavigation?.Cargo ?? "USUARIO"; // Si no tiene cargo, se le asigna "USUARIO"
            var token = GenerateJwtToken(user, cargo);
            int almacenId = user.IdPersonalNavigation?.IdAlmacen ?? 0;

            return Ok(new
            {
                Token = token,
                UserId = user.IdUsuario,
                Nombre = user.Nombre,
                Cargo = cargo,
                IdAlmacen = almacenId
            });
        }

        // Este método simula la obtención de la contraseña desde la base de datos
        private async Task<string?> GetPasswordFromDB(string usuario)
        {
            string? password = null;

            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al conectar a la base de datos: {ex.Message}");
                    return null;
                }  // Esto asegura que la conexión está abierta antes de ejecutar el comando

                using (var command = new SqlCommand("dbo.usp_get_password", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@usuario", usuario);

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        password = result.ToString();
                    }
                }
            }

            return password;
        }

        private string GenerateJwtToken(Usuario user, string cargo)
        {
            var keyString = _configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(keyString))
            {
                throw new ArgumentNullException(nameof(keyString), "La clave JWT no puede ser nula o vacía.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Nombre),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserId", user.IdUsuario.ToString()),
                new Claim("Cargo", cargo) // Agregamos el cargo al token
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            Console.WriteLine($"Token generado en backend: {jwt}");
            return jwt;
        }
    }

    public class LoginRequest
    {
        public required string Usuario { get; set; }
        public required string Password { get; set; }
    }
}