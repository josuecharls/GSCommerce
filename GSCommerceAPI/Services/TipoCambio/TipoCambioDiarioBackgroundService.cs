using GSCommerceAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.TipoCambio;

public class TipoCambioDiarioBackgroundService : BackgroundService
{
    private readonly ILogger<TipoCambioDiarioBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<TipoCambioDiarioOptions> _optionsMonitor;
    private DateOnly? _ultimaFechaProcesada;

    public TipoCambioDiarioBackgroundService(
        ILogger<TipoCambioDiarioBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<TipoCambioDiarioOptions> optionsMonitor)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de réplica diaria de tipo de cambio iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Ignorar cancelación explícita.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al replicar el tipo de cambio diario.");
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

        _logger.LogInformation("Servicio de réplica diaria de tipo de cambio detenido.");
    }

    private async Task ProcesarAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled)
            return;

        var ahora = DateTime.Now;
        var horaEjecucion = Math.Clamp(options.RunHour, 0, 23);
        var minutoEjecucion = Math.Clamp(options.RunMinute, 0, 59);
        var inicioVentana = new TimeSpan(horaEjecucion, minutoEjecucion, 0);
        var hoy = DateOnly.FromDateTime(ahora);

        if (_ultimaFechaProcesada == hoy)
            return;

        if (ahora.TimeOfDay < inicioVentana)
            return;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SyscharlesContext>();
        var enVentanaActualizacion = ahora.TimeOfDay < inicioVentana.Add(TimeSpan.FromHours(1));

        if (!enVentanaActualizacion)
        {
            var existeHoy = await context.TipoDeCambios.AnyAsync(t => t.Fecha == hoy, stoppingToken);
            if (existeHoy)
            {
                _ultimaFechaProcesada = hoy;
                return;
            }
        }

        var servicio = scope.ServiceProvider.GetRequiredService<TipoCambioDiarioService>();
        var resultado = await servicio.ReplicarAsync(hoy, stoppingToken);

        if (string.Equals(resultado.Accion, "SIN_ORIGEN", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "No hay tipo de cambio anterior para replicar al {Fecha:yyyy-MM-dd}.",
                hoy);
        }
        else
        {
            _logger.LogInformation(
                "Tipo de cambio {Accion} para {FechaDestino:yyyy-MM-dd} desde {FechaOrigen:yyyy-MM-dd}. Compra: {Compra}, Venta: {Venta}.",
                resultado.Accion,
                resultado.FechaDestino,
                resultado.FechaOrigen,
                resultado.Compra,
                resultado.Venta);
        }

        _ultimaFechaProcesada = hoy;
    }
}
