using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GSCommerce.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly AuthService _authService;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthStateProvider(ILocalStorageService localStorage, AuthService authService)
        {
            _localStorage = localStorage;
            _authService = authService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                var cargo = await _localStorage.GetItemAsync<string>("Cargo");

                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(cargo))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken;
                
                try
                {
                    jwtToken = handler.ReadJwtToken(token);
                }
                catch (Exception ex)
                {
                    // Token inválido o corrompido
                    Console.WriteLine($"Token inválido: {ex.Message}");
                    await MarkUserAsLoggedOut();
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Verificar si el token está próximo a expirar (5 minutos antes)
                var timeUntilExpiry = jwtToken.ValidTo - DateTime.UtcNow;
                if (timeUntilExpiry <= TimeSpan.FromMinutes(5) || jwtToken.ValidTo <= DateTime.UtcNow)
                {
                    var refreshed = await _authService.RefreshTokenAsync();
                    if (!refreshed)
                    {
                        await MarkUserAsLoggedOut();
                        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                    }

                    token = await _localStorage.GetItemAsync<string>("authToken");
                    if (string.IsNullOrEmpty(token))
                    {
                        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                    }
                    jwtToken = handler.ReadJwtToken(token);
                    cargo = await _localStorage.GetItemAsync<string>("Cargo");
                }

                var claims = jwtToken.Claims.ToList();
                if (!string.IsNullOrEmpty(cargo))
                {
                    claims.Add(new Claim("Cargo", cargo));
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                _currentUser = new ClaimsPrincipal(identity);

                return new AuthenticationState(_currentUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetAuthenticationStateAsync: {ex.Message}");
                // En caso de error, intentar limpiar y retornar estado no autenticado
                try
                {
                    await MarkUserAsLoggedOut();
                }
                catch { }
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task MarkUserAsAuthenticated(string token, string cargo)
        {
            // Console.WriteLine($" Token recibido en MarkUserAsAuthenticated: {token}");
            // Console.WriteLine($" Cargo recibido en MarkUserAsAuthenticated: {cargo}");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(cargo))
            {
                Console.WriteLine("❌ Error: Token o Cargo son nulos.");
                return;
            }

            var claims = ParseClaimsFromJwt(token).ToList();
            claims.Add(new Claim("Cargo", cargo));

            var identity = new ClaimsIdentity(claims, "jwt");
            _currentUser = new ClaimsPrincipal(identity);

            await _localStorage.SetItemAsync("authToken", token);
            await _localStorage.SetItemAsync("Cargo", cargo);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            await _localStorage.RemoveItemAsync("Cargo");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task<string?> GetUserCargo()
        {
            return await _localStorage.GetItemAsync<string>("Cargo");
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string token)
        {
            var claims = new List<Claim>();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

             //   Console.WriteLine("Token JWT correctamente parseado.");
                return jwtToken.Claims;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al parsear el token JWT: {ex.Message}");
                return claims;
            }
        }

        //Función para asegurar que el Base64 tiene padding correcto
        private string PadBase64String(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: return base64 + "==";
                case 3: return base64 + "=";
                default: return base64;
            }
        }
    }
}