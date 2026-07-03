using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GSCommerce.Client.Models.DTOs.Reportes;
using GSCommerceAPI.Data;
using GSCommerceAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.Reportes;

public class ReporteAvanceHoraEmailBackgroundService : BackgroundService
{
    private readonly ILogger<ReporteAvanceHoraEmailBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<ReporteAvanceHoraEmailOptions> _optionsMonitor;
    private int _ultimaHoraProcesada = -1;

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
        
        // Evitar el envío inmediato si el servicio se reinicia a la mitad de una hora
        // asumiendo que el reporte ya se envió a su debida hora.
        _ultimaHoraProcesada = DateTime.Now.Hour;

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
                // Se agrega un margen de seguridad de 2 segundos para evitar que 
                // Task.Delay despierte milisegundos antes de la hora (early wakeup), 
                // lo que causaba el envío duplicado del reporte de la hora anterior 
                // y el salto de la hora actual.
                await Task.Delay(espera.Add(TimeSpan.FromSeconds(2)), stoppingToken);
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
        
        if (_ultimaHoraProcesada == ahora.Hour)
        {
            _logger.LogDebug("El reporte para la hora {Hora} ya fue procesado o se omitió por reinicio.", ahora.Hour);
            return;
        }

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
        var emailService = scope.ServiceProvider.GetRequiredService<IReporteAvanceHoraEmailService>();
        var context = scope.ServiceProvider.GetRequiredService<SyscharlesContext>();

        var almacenesConfigurados = options.WarehouseIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();
        var destinos = await ObtenerAlmacenesDestinoAsync(context, almacenesConfigurados, stoppingToken);

        if (!destinos.Any())
        {
            _logger.LogWarning("No se encontraron almacenes para enviar el reporte de avance por hora. Se enviará el resumen general.");

            stoppingToken.ThrowIfCancellationRequested();
            var reporteGeneral = await provider.ObtenerReporteAsync(inicio, fin, null, null, stoppingToken);

            if (!TieneVentas(reporteGeneral))
            {
                _logger.LogInformation(
                    "No se envía el reporte general de avance por hora: sin ventas acumuladas al {Hora:dd/MM/yyyy HH:mm}.",
                    fin);
                _ultimaHoraProcesada = ahora.Hour;
                return;
            }

            await emailService.EnviarReporteAsync(
                new[] { new ReporteAvanceHoraEmailEntry(reporteGeneral, "Todos los almacenes", null) },
                stoppingToken);
                
            _ultimaHoraProcesada = ahora.Hour;
            return;
        }

        var reportes = new List<ReporteAvanceHoraEmailEntry>();

        foreach (var destino in destinos)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var reporte = await provider.ObtenerReporteAsync(inicio, fin, destino.IdAlmacen, destino.IdAlmacen, stoppingToken);

            if (!TieneVentas(reporte))
            {
                _logger.LogDebug(
                    "Almacén {Almacen} (Id {IdAlmacen}) omitido del correo: sin ventas acumuladas al {Hora:dd/MM/yyyy HH:mm}.",
                    destino.Nombre,
                    destino.IdAlmacen,
                    fin);
                continue;
            }

            reportes.Add(new ReporteAvanceHoraEmailEntry(reporte, destino.Nombre, destino.IdAlmacen));
        }

        if (!reportes.Any())
        {
            _logger.LogInformation(
                "No se envía el correo de avance por hora: ningún almacén con ventas acumuladas al {Hora:dd/MM/yyyy HH:mm}.",
                fin);
            _ultimaHoraProcesada = ahora.Hour;
            return;
        }

        await emailService.EnviarReporteAsync(reportes, stoppingToken);
        _ultimaHoraProcesada = ahora.Hour;
    }

    private static bool TieneVentas(ReporteAvanceHoraDTO reporte) =>
        reporte.TotalVentasDia > 0m;

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

    private static async Task<List<AlmacenDestino>> ObtenerAlmacenesDestinoAsync(
        SyscharlesContext context,
        List<int> almacenesConfigurados,
        CancellationToken cancellationToken)
    {
        if (almacenesConfigurados.Count > 0)
        {
            var almacenes = await context.Almacens
                .AsNoTracking()
                .Where(a => almacenesConfigurados.Contains(a.IdAlmacen))
                .Select(a => new { a.IdAlmacen, a.Nombre })
                .ToListAsync(cancellationToken);

            var nombres = almacenes.ToDictionary(
                a => a.IdAlmacen,
                a => string.IsNullOrWhiteSpace(a.Nombre) ? (string?)null : a.Nombre);

            return almacenesConfigurados
                .Select(id => new AlmacenDestino(id, nombres.TryGetValue(id, out var nombre) && nombre != null ? nombre : $"Almacén {id}"))
                .ToList();
        }

        return await context.Almacens
            .AsNoTracking()
            .Where(a => a.Estado)
            .OrderBy(a => a.Nombre)
            .Select(a => new AlmacenDestino(a.IdAlmacen, string.IsNullOrWhiteSpace(a.Nombre) ? $"Almacén {a.IdAlmacen}" : a.Nombre))
            .ToListAsync(cancellationToken);
    }

    private sealed record AlmacenDestino(int IdAlmacen, string Nombre);
}