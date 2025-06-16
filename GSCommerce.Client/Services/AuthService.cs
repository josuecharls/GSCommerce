using Blazored.LocalStorage;
using GSCommerce.Client.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;

namespace GSCommerce.Client.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private ClaimsPrincipal? _currentUser;

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async Task<bool> Login(LoginRequest loginRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
            if (result != null && !string.IsNullOrEmpty(result.Token))
            {
                Console.WriteLine($" Token recibido en AuthService: {result.Token}");

                await _localStorage.SetItemAsync("authToken", result.Token);
                await _localStorage.SetItemAsync("userName", result.Nombre);
                await _localStorage.SetItemAsync("userId", result.UserId);
                await _localStorage.SetItemAsync("IdAlmacen", result.IdAlmacen);
                await _localStorage.SetItemAsync("Cargo", result.Cargo);

                return true;
            }
            return false;
        }

        public async Task<int?> GetUserAlmacenId()
        {
            return await _localStorage.GetItemAsync<int>("IdAlmacen");
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userName");
            await _localStorage.RemoveItemAsync("userId");
            await _localStorage.RemoveItemAsync("Cargo"); // Elimina el Cargo al cerrar sesión
            _currentUser = null;
        }

        public async Task<string?> GetNombrePersonal()
        {
            var userId = await _localStorage.GetItemAsync<string>("userId");
            if (string.IsNullOrEmpty(userId))
                return null;

            var response = await _httpClient.GetAsync($"api/usuarios/info/{userId}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> IsAuthenticated()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string?> GetUserName()
        {
            return await _localStorage.GetItemAsync<string>("userName");
        }

        public async Task<int?> GetUserId()
        {
            return await _localStorage.GetItemAsync<int>("userId");
        }

        public async Task<string?> GetUserCargo()
        {
            return await _localStorage.GetItemAsync<string>("Cargo"); // Método para obtener el Cargo del usuario
        }

        private ClaimsPrincipal ParseToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }

        public async Task<DatosEmisor?> ObtenerDatosEmisorAsync()
        {
            return await _httpClient.GetFromJsonAsync<DatosEmisor>("api/ventas/emisor");
        }
    }

    public class DatosEmisor
    {
        public string Ruc { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Ubigeo { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public required string Usuario { get; set; }
        public required string Password { get; set; }
    }

    public class AuthResponse
    {
        public required string Token { get; set; }
        public int UserId { get; set; }
        public required string Nombre { get; set; }
        public required string Cargo { get; set; } // Se añade el Cargo en la respuesta
        public int IdAlmacen { get; set; }  // Se añade el almacen
    }
}