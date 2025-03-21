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
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddAuthorizationCore();


// Configurar HttpClient para conectarse al backend
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7246") });

await builder.Build().RunAsync();