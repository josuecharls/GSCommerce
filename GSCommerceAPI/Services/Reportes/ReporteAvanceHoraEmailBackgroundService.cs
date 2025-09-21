using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace GSCommerceAPI.Services.Reportes;

public class ReporteAvanceHoraEmailBackgroundService : BackgroundService
{
    private readonly ILogger<ReporteAvanceHoraEmailBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<ReporteAvanceHoraEmailOptions> _optionsMonitor;

    public ReporteAvanceHoraEmailBackgroundService(
        ILogger<ReporteAvanceHoraEmailBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<ReporteAvanceHoraEmailOptions> optionsMonitor)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de envío de reporte de avance por hora iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarEnvioAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignorar cancelación explícita.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el envío del reporte de avance por hora.");
            }

            var espera = CalcularEsperaHastaSiguienteHora();

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Servicio de envío de reporte de avance por hora detenido.");
    }

    private async Task ProcesarEnvioAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogDebug("El envío automático del reporte de avance por hora está deshabilitado por configuración.");
            return;
        }

        var ahora = DateTime.Now;
        if (!EstaDentroDeVentana(ahora, options))
        {
            _logger.LogDebug(
                "La hora actual ({Hora}) está fuera del rango configurado ({Inicio}-{Fin}).",
                ahora.ToString("HH:mm"),
                options.SendStartHour,
                options.SendEndHour);
            return;
        }

        var fin = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, 0, 0);
        var inicio = fin.AddHours(-1);

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ReporteAvanceHoraProvider>();
        var emailService = scope.ServiceProvider.GetRequiredService<ReporteAvanceHoraEmailService>();
        var context = scope.ServiceProvider.GetRequiredService<SyscharlesContext>();

        var almacenesConfigurados = options.WarehouseIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        var almacenes = almacenesConfigurados.Count > 0
            ? almacenesConfigurados.Select(id => (int?)id).ToList()
            : new List<int?> { null };

        foreach (var idAlmacen in almacenes)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var reporte = await provider.ObtenerReporteAsync(inicio, fin, idAlmacen, null, stoppingToken);
            var nombreAlmacen = await ObtenerNombreAlmacenAsync(context, idAlmacen, stoppingToken);

            await emailService.EnviarReporteAsync(reporte, nombreAlmacen, idAlmacen, stoppingToken);
        }
    }

    private static async Task<string?> ObtenerNombreAlmacenAsync(SyscharlesContext context, int? idAlmacen, CancellationToken cancellationToken)
    {
        if (!idAlmacen.HasValue)
        {
            return "Todos los almacenes";
        }

        return await context.Almacens
            .Where(a => a.IdAlmacen == idAlmacen.Value)
            .Select(a => a.Nombre)
            .FirstOrDefaultAsync(cancellationToken)
            ?? $"Almacén {idAlmacen.Value}";
    }

    private static bool EstaDentroDeVentana(DateTime momento, ReporteAvanceHoraEmailOptions options)
    {
        var inicio = Math.Clamp(options.SendStartHour, 0, 23);
        var fin = Math.Clamp(options.SendEndHour, 0, 23);

        if (inicio <= fin)
        {
            return momento.Hour >= inicio && momento.Hour <= fin;
        }

        // Si el fin es menor al inicio se asume una ventana que cruza medianoche.
        return momento.Hour >= inicio || momento.Hour <= fin;
    }

    private static TimeSpan CalcularEsperaHastaSiguienteHora()
    {
        var ahora = DateTime.Now;
        var siguienteHora = new DateTime(ahora.Year, ahora.Month, ahora.Day, ahora.Hour, 0, 0).AddHours(1);
        var espera = siguienteHora - ahora;
        return espera > TimeSpan.Zero ? espera : TimeSpan.Zero;
    }
}