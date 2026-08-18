using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GSCommerceAPI.Services;
using GSCommerceAPI.Services.Caja;
using GSCommerceAPI.Services.Reportes;
using GSCommerceAPI.Services.SUNAT;
using GSCommerceAPI.Services.TipoCambio;
using ServicioSunat;
using AspNetCoreRateLimit;



QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.FontDiscoveryPaths.Clear();
QuestPDF.Settings.FontDiscoveryPaths.Add(Path.Combine(AppContext.BaseDirectory, "Fuentes"));

var nombreFuente = "ARIAL.TTF";
var rutaFuente = Path.Combine(AppContext.BaseDirectory, "Fuentes", nombreFuente);

if (File.Exists(rutaFuente))
{
    using var stream = File.OpenRead(rutaFuente);
    QuestPDF.Drawing.FontManager.RegisterFont(stream);
    Console.WriteLine($"[INFO] Fuente cargada correctamente desde: {rutaFuente}");
}
else
{
    Console.WriteLine($"[PELIGRO] No se encontró el archivo de fuente en: {rutaFuente}");
}

var builder = WebApplication.CreateBuilder(args);

// Configurar la conexi�n con SQL Server usando SyscharlesContext
builder.Services.AddDbContext<SyscharlesContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql => sql.UseCompatibilityLevel(120)));

builder.Services.AddDbContext<SysDbHistoricalContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HistoricalConnection"), sql => sql.UseCompatibilityLevel(120))
           .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
builder.Services.Configure<ReporteAvanceHoraEmailOptions>(builder.Configuration.GetSection(ReporteAvanceHoraEmailOptions.SectionName));
builder.Services.Configure<CuadroCajaEmailOptions>(builder.Configuration.GetSection(CuadroCajaEmailOptions.SectionName));
builder.Services.Configure<TipoCambioDiarioOptions>(builder.Configuration.GetSection(TipoCambioDiarioOptions.SectionName));
Console.WriteLine(
    "Cadena de conexi�n: " + builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddScoped<IFacturacionElectronicaService, FacturacionElectronicaService>();
builder.Services.AddScoped<IGuiaRemisionElectronicaService, GuiaRemisionElectronicaService>();
builder.Services.AddScoped<ReporteAvanceHoraProvider>();
builder.Services.AddScoped<IReporteAvanceHoraEmailService, ReporteAvanceHoraEmailService>();
builder.Services.AddScoped<ICajaVentasDiariasService, CajaVentasDiariasService>();
builder.Services.AddScoped<ICuadroCajaReportProvider, CuadroCajaReportProvider>();
builder.Services.AddScoped<ICuadroCajaEmailService, CuadroCajaEmailService>();
builder.Services.AddScoped<TipoCambioDiarioService>();
builder.Services.AddScoped<GSCommerceAPI.Services.PasswordHasherService>();
builder.Services.AddHostedService<TicketValidationBackgroundService>();
builder.Services.AddHostedService<ReporteAvanceHoraEmailBackgroundService>();
builder.Services.AddHostedService<CuadroCajaEmailBackgroundService>();
builder.Services.AddHostedService<TipoCambioDiarioBackgroundService>();

// Habilitar CORS para permitir llamadas desde el frontend
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                     ?? (builder.Environment.IsDevelopment()
                         ? new[] { "https://gscommerce.net/" }
                         : Array.Empty<string>());

if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException("Debe configurar al menos un origen permitido en 'AllowedOrigins' para producción.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy => policy.WithOrigins(allowedOrigins)
                        .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                        .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                        .AllowCredentials());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024; // 5 MB

    // Producción detrás de Nginx: Kestrel solo HTTP (Nginx termina HTTPS público).
    if (!builder.Environment.IsDevelopment())
    {
        options.ListenAnyIP(5000);
    }
});

// Configurar la autenticaci�n con JWT
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new ArgumentNullException("JwtSettings:SecretKey", "La clave JWT no puede ser nula o vacía.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

// Rate limiting para protección contra fuerza bruta
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "GSCommerce API",
        Version = "v1",
        Description = "API para el sistema GSCommerce"
    });

    // Configurar JWT Bearer en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Habilitar CORS
app.UseCors("AllowBlazor");

// Rate limiting (antes de autenticación para proteger endpoints públicos)
app.UseIpRateLimiting();

// HTTPS lo termina Nginx; no redirigir aquí (rompería proxy_pass HTTP interno).

app.UseAuthentication();
app.UseAuthorization();
// Middleware de manejo global de excepciones
app.UseMiddleware<GSCommerceAPI.Middleware.GlobalExceptionHandlerMiddleware>();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering(); // Permite leer el cuerpo de la solicitud m�s de una vez
    await next();
});
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Run(); // En desarrollo, Kestrel ya está configurado para escuchar en el puerto correcto
}
else
{
    // En producción, Kestrel ya está configurado para escuchar en los puertos correctos
    app.Run();
}