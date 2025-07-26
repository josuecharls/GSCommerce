using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GSCommerceAPI.Services.SUNAT;
using ServicioSunat;



QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// Configurar la conexión con SQL Server usando SyscharlesContext
builder.Services.AddDbContext<SyscharlesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
Console.WriteLine(
    "Cadena de conexión: " + builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
builder.Services.AddHostedService<TicketValidationBackgroundService>();
// Habilitar CORS para permitir llamadas desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.WithOrigins("https://gscommerce.net/") // URL del frontend Blazor
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB
});

// Configurar la autenticación con JWT
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new ArgumentNullException("JwtSettings:SecretKey", "La clave JWT no puede ser nula o vacía.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

// Habilitar CORS
app.UseCors("AllowBlazor");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering(); // Permite leer el cuerpo de la solicitud más de una vez
    await next();
});
app.MapControllers();
try
{
    app.Run("http://0.0.0.0:5000");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR CRÍTICO EN LA API: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}