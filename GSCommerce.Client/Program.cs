using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GSCommerce.Client;
using GSCommerce.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<PersonalService>();
builder.Services.AddScoped<AlmacenService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<ArticuloService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<MovimientoGuiaService>();
builder.Services.AddScoped<OrdenCompraService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ArticuloVarianteService>();
builder.Services.AddScoped<KardexService>();
builder.Services.AddScoped<KardexDetalladoService>();
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<SerieCorrelativoService>();
builder.Services.AddScoped<TipoDocumentoVentaService>();
builder.Services.AddScoped<TomaInventariosService>();
builder.Services.AddScoped<AlmacenCuentaService>();
builder.Services.AddScoped<ReniecService>();
builder.Services.AddScoped<CajaService>();
builder.Services.AddScoped<AuxiliaresVariosService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<DescuentoService>();
builder.Services.AddScoped<AsignacionSerieCajeroService>();
builder.Services.AddScoped<ReporteService>();
builder.Services.AddScoped<ResumenSunatService>();
builder.Services.AddScoped<ResumenService>();
builder.Services.AddScoped<TipoCambioService>();
builder.Services.AddScoped<MonedaAlmacenService>();
builder.Services.AddScoped<NotaCreditoService>();
builder.Services.AddScoped<IngresosEgresosService>();
builder.Services.AddAuthorizationCore();

// Configurar HttpClient para conectarse al backend
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://gscommerce.net/api/") });

var host = builder.Build();

var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
var token = await localStorage.GetItemAsync<string>("authToken");

if (!string.IsNullOrWhiteSpace(token))
{
    var httpClient = host.Services.GetRequiredService<HttpClient>();
    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
}

await host.RunAsync();