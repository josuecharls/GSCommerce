using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GSCommerceAPI.Services;
using GSCommerceAPI.Services.Reportes;
using GSCommerceAPI.Services.SUNAT;
using ServicioSunat;



QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var builder = WebApplication.CreateBuilder(args);

// Configurar la conexi�n con SQL Server usando SyscharlesContext
builder.Services.AddDbContext<SyscharlesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.Configure<ReporteAvanceHoraEmailOptions>(builder.Configuration.GetSection(ReporteAvanceHoraEmailOptions.SectionName));
Console.WriteLine(
    "Cadena de conexi�n: " + builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
builder.Services.AddScoped<IGuiaRemisionElectronicaService, GuiaRemisionElectronicaService>();
builder.Services.AddScoped<ReporteAvanceHoraProvider>();
builder.Services.AddScoped<IReporteAvanceHoraEmailService, ReporteAvanceHoraEmailService>();
builder.Services.AddHostedService<TicketValidationBackgroundService>();
builder.Services.AddHostedService<ReporteAvanceHoraEmailBackgroundService>();

// Habilitar CORS para permitir llamadas desde el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.WithOrigins("https://localhost:7018") // URL del frontend Blazor
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB
});

// Configurar la autenticaci�n con JWT
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new ArgumentNullException("JwtSettings:SecretKey", "La clave JWT no puede ser nula o vac�a.");
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
    context.Request.EnableBuffering(); // Permite leer el cuerpo de la solicitud m�s de una vez
    await next();
});
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Run(); // usar� https://localhost:7246; http://localhost:5051 de launchSettings.json
}
else
{
    app.Run("http://0.0.0.0:5000");
}