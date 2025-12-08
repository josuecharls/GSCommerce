using GSCommerceAPI.Data;
using GSCommerceAPI.Models;
using GSCommerceAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
        private readonly PasswordHasherService _passwordHasher;

        public AuthController(SyscharlesContext context, IConfiguration configuration, PasswordHasherService passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Usuario) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Usuario y contraseña requeridos.");
            }

            var storedPassword = await GetPasswordFromDB(request.Usuario);
            if (storedPassword == null || !_passwordHasher.VerifyPassword(request.Password, storedPassword))
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
            var refreshToken = GenerateRefreshToken();

            // Guardar refresh token en base de datos
            await SaveRefreshTokenToDB(user.IdUsuario, refreshToken);

            int almacenId = user.IdPersonalNavigation?.IdAlmacen ?? 0;

            return Ok(new
            {
                Token = token,
                RefreshToken = refreshToken,
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

        private async Task SaveRefreshTokenToDB(int userId, string token)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Token válido por 7 días
                CreatedAt = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
        }

        private async Task<RefreshToken?> GetRefreshTokenFromDB(int userId, string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);
        }

        private async Task DeleteRefreshTokenFromDB(int userId, string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);

            if (refreshToken != null)
            {
                _context.RefreshTokens.Remove(refreshToken);
                await _context.SaveChangesAsync();
            }
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
                new Claim("userId", user.IdUsuario.ToString()),
                new Claim("Cargo", cargo), // Claim personalizado
                new Claim(ClaimTypes.Role, cargo) // Claim estándar para roles
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var existingToken = await GetRefreshTokenFromDB(request.UserId, request.RefreshToken);
            if (existingToken == null || existingToken.ExpiresAt < DateTime.UtcNow)
            {
                return Unauthorized("Refresh token inválido o expirado.");
            }

            var user = await _context.Usuarios
                .Include(u => u.IdPersonalNavigation)
                .FirstOrDefaultAsync(u => u.IdUsuario == request.UserId);

            if (user == null || !user.Estado)
            {
                return Unauthorized("Usuario inactivo o no encontrado.");
            }

            string cargo = user.IdPersonalNavigation?.Cargo ?? "USUARIO";
            var token = GenerateJwtToken(user, cargo);
            var newRefreshToken = GenerateRefreshToken();

            // Eliminar token antiguo y guardar nuevo
            await DeleteRefreshTokenFromDB(request.UserId, request.RefreshToken);
            await SaveRefreshTokenToDB(user.IdUsuario, newRefreshToken);
            int almacenId = user.IdPersonalNavigation?.IdAlmacen ?? 0;

            return Ok(new
            {
                Token = token,
                RefreshToken = newRefreshToken,
                UserId = user.IdUsuario,
                Nombre = user.Nombre,
                Cargo = cargo,
                IdAlmacen = almacenId
            });
        }
    }

    public class LoginRequest
    {
        public required string Usuario { get; set; }
        public required string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        public int UserId { get; set; }
        public required string RefreshToken { get; set; }
    }
}