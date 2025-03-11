using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace GSCommerce.Client.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var cargo = await _localStorage.GetItemAsync<string>("Cargo");

            Console.WriteLine($"🔹 Token recuperado de LocalStorage: {token}");
            Console.WriteLine($"🔹 Cargo recuperado de LocalStorage: {cargo}");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(cargo))
            {
                Console.WriteLine("❌ No hay token o cargo, usuario no autenticado.");
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())); // Usuario no autenticado
            }

            var claims = ParseClaimsFromJwt(token).ToList();
            claims.Add(new Claim("Cargo", cargo));

            var identity = new ClaimsIdentity(claims, "jwt");
            _currentUser = new ClaimsPrincipal(identity);

            Console.WriteLine("✅ Usuario restaurado correctamente desde el token.");
            return new AuthenticationState(_currentUser);
        }

        public async Task MarkUserAsAuthenticated(string token, string cargo)
        {
            Console.WriteLine($"🔹 Token recibido en MarkUserAsAuthenticated: {token}");
            Console.WriteLine($"🔹 Cargo recibido en MarkUserAsAuthenticated: {cargo}");

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
            await _localStorage.RemoveItemAsync("Cargo");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task<string?> GetUserCargo()
        {
            return await _localStorage.GetItemAsync<string>("Cargo"); //  Obtener Cargo de LocalStorage
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string token)
        {
            var claims = new List<Claim>();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                Console.WriteLine("✅ Token JWT correctamente parseado.");
                return jwtToken.Claims;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error al parsear el token JWT: {ex.Message}");
                return claims; // Devolver una lista vacía si hay un error
            }
        }

        // ✅ Función para asegurar que el Base64 tiene padding correcto
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