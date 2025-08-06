using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;

namespace GSCommerceAPI.Services.SUNAT
{
    public class TicketValidationBackgroundService : BackgroundService
    {
        private readonly ILogger<TicketValidationBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private const int IntervalMinutes = 5;

        public TicketValidationBackgroundService(ILogger<TicketValidationBackgroundService> logger,
                                                 IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ValidatePendingTicketsAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(IntervalMinutes), stoppingToken);
            }
        }

        private async Task ValidatePendingTicketsAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SyscharlesContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var feService = scope.ServiceProvider.GetRequiredService<IFacturacionElectronicaService>();

            var pendientes = await context.Resumen
                .Where(r => r.TicketSunat != null && (r.RespuestaSunat == null || r.RespuestaSunat == ""))
                .ToListAsync(token);

            foreach (var resumen in pendientes)
            {
                string? ruc = null;
                try
                {
                    var nombreBase = Path.GetFileNameWithoutExtension(resumen.NombreArchivo);
                    string zipPath = Path.Combine(env.ContentRootPath, "Facturacion", nombreBase + ".zip");
                    ruc = nombreBase.Split('-').First();

                    var credenciales = await context.Almacens
                        .Where(a => a.Ruc == ruc)
                        .Select(a => new { a.UsuarioSol, a.ClaveSol })
                        .FirstOrDefaultAsync(token);

                    if (credenciales == null || string.IsNullOrEmpty(credenciales.UsuarioSol) || string.IsNullOrEmpty(credenciales.ClaveSol))
                    {
                        _logger.LogWarning("No se encontraron credenciales SOL para el RUC {Ruc}", ruc);
                        continue;
                    }

                    var usuarioSOL = $"{ruc}{credenciales.UsuarioSol}";
                    var respuesta = await feService.ValidarTicketSunatAsync(resumen.TicketSunat, zipPath, usuarioSOL, credenciales.ClaveSol);

                    resumen.RespuestaSunat = respuesta;
                    resumen.FechaRespuestaSunat = DateTime.Now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error al validar ticket {Ticket} del archivo {NombreArchivo} y RUC {Ruc}",
                        resumen.TicketSunat,
                        resumen.NombreArchivo,
                        ruc);
                }
            }

            await context.SaveChangesAsync(token);
        }
    }
}
