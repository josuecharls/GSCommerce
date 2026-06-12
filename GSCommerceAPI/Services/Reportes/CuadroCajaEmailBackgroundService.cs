using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.Reportes;

public class CuadroCajaEmailBackgroundService : BackgroundService
{
    private readonly ILogger<CuadroCajaEmailBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<CuadroCajaEmailOptions> _optionsMonitor;
    private DateOnly? _ultimaFechaEnviada;

    public CuadroCajaEmailBackgroundService(
        ILogger<CuadroCajaEmailBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<CuadroCajaEmailOptions> optionsMonitor)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de envío de cuadros de caja por correo iniciado.");

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
                _logger.LogError(ex, "Error al procesar el envío de cuadros de caja por correo.");
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

        _logger.LogInformation("Servicio de envío de cuadros de caja por correo detenido.");
    }

    private async Task ProcesarEnvioAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
            return;

        var ahora = DateTime.Now;
        var horaEnvio = Math.Clamp(options.SendHour, 0, 23);
        var minutoEnvio = Math.Clamp(options.SendMinute, 0, 59);

        if (ahora.Hour != horaEnvio || ahora.Minute != minutoEnvio)
            return;

        var fechaReporte = DateOnly.FromDateTime(ahora);
        if (_ultimaFechaEnviada == fechaReporte)
            return;

        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ICuadroCajaReportProvider>();
        var emailService = scope.ServiceProvider.GetRequiredService<ICuadroCajaEmailService>();

        var ventasEfectivo = await provider.ObtenerCuadroVentasEfectivoAsync(fechaReporte, stoppingToken);
        if (ventasEfectivo.Filas.Count == 0)
        {
            _logger.LogWarning("No hay datos de caja para enviar cuadros del {Fecha:yyyy-MM-dd}.", fechaReporte);
            return;
        }

        var billetera = await provider.ObtenerBilleteraVirtualAsync(fechaReporte, stoppingToken);

        await emailService.EnviarCuadrosAsync(ventasEfectivo, billetera, stoppingToken);
        _ultimaFechaEnviada = fechaReporte;
    }
}
