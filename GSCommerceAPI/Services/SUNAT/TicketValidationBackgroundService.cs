using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;

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
                try
                {
                    string zipPath = Path.Combine(env.ContentRootPath, "Facturacion", resumen.NombreArchivo + ".zip");
                    var respuesta = await feService.ValidarTicketSunatAsync(resumen.TicketSunat, zipPath, "", "");
                    resumen.RespuestaSunat = respuesta;
                    resumen.FechaRespuestaSunat = DateTime.Now;
                    await context.SaveChangesAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al validar ticket {Ticket}", resumen.TicketSunat);
                }
            }
        }
    }
}
