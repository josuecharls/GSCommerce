using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.Reportes;

public class AproximacionGastosEmailBackgroundService : BackgroundService
{
    private readonly ILogger<AproximacionGastosEmailBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<AproximacionGastosEmailOptions> _optionsMonitor;
    private DateOnly? _ultimaFechaEnviada;

    public AproximacionGastosEmailBackgroundService(
        ILogger<AproximacionGastosEmailBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<AproximacionGastosEmailOptions> optionsMonitor)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de envío de aproximación de gastos por correo iniciado.");

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
                _logger.LogError(ex, "Error al procesar el envío de aproximación de gastos por correo.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Servicio de envío de aproximación de gastos por correo detenido.");
    }

    private async Task ProcesarEnvioAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
            return;

        var ahora = DateTime.Now;
        var fechaHoy = DateOnly.FromDateTime(ahora);

        if (!EsDiaDeEnvio(fechaHoy))
            return;

        var horaEnvio = Math.Clamp(options.SendHour, 0, 23);
        var minutoEnvio = Math.Clamp(options.SendMinute, 0, 59);

        if (ahora.Hour != horaEnvio || ahora.Minute != minutoEnvio)
            return;

        if (_ultimaFechaEnviada == fechaHoy)
            return;

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IAproximacionGastosReportProvider>();
        var emailService = scope.ServiceProvider.GetRequiredService<IAproximacionGastosEmailService>();

        var reporte = await provider.ObtenerReporteAsync(fechaHoy, stoppingToken);
        if (reporte.Filas.Count == 0)
        {
            _logger.LogWarning(
                "No hay tiendas con ventas para enviar aproximación de gastos del {Desde:yyyy-MM-dd} al {Hasta:yyyy-MM-dd}.",
                reporte.Desde,
                reporte.Hasta);
            return;
        }

        await emailService.EnviarAsync(reporte, stoppingToken);
        _ultimaFechaEnviada = fechaHoy;
    }

    internal static bool EsDiaDeEnvio(DateOnly fecha)
    {
        if (fecha.Day == 15)
            return true;

        var ultimoDia = DateTime.DaysInMonth(fecha.Year, fecha.Month);
        return fecha.Day == ultimoDia;
    }
}
