using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GSCommerce.Client.Models.DTOs.Reportes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services;

public sealed class ReporteAvanceHoraEmailEntry
{
    public ReporteAvanceHoraEmailEntry(ReporteAvanceHoraDTO reporte, string? nombreAlmacen, int? idAlmacen)
    {
        Reporte = reporte ?? throw new ArgumentNullException(nameof(reporte));
        NombreAlmacen = nombreAlmacen;
        IdAlmacen = idAlmacen;
    }

    public ReporteAvanceHoraDTO Reporte { get; }

    public string? NombreAlmacen { get; }

    public int? IdAlmacen { get; }
}

public interface IReporteAvanceHoraEmailService
{
    Task EnviarReporteAsync(IReadOnlyCollection<ReporteAvanceHoraEmailEntry> reportes, CancellationToken cancellationToken = default);
}

public class ReporteAvanceHoraEmailService : IReporteAvanceHoraEmailService
{
    private readonly ILogger<ReporteAvanceHoraEmailService> _logger;
    private readonly IOptionsMonitor<ReporteAvanceHoraEmailOptions> _optionsMonitor;

    public ReporteAvanceHoraEmailService(
        ILogger<ReporteAvanceHoraEmailService> logger,
        IOptionsMonitor<ReporteAvanceHoraEmailOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async Task EnviarReporteAsync(IReadOnlyCollection<ReporteAvanceHoraEmailEntry> reportes, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogDebug("El envío de correos de avance por hora está deshabilitado por configuración.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.From) || string.IsNullOrWhiteSpace(options.Password))
        {
            _logger.LogWarning("Las credenciales del remitente no están configuradas correctamente.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            _logger.LogWarning("El servidor SMTP no está configurado.");
            return;
        }

        if (options.To == null || options.To.Count == 0)
        {
            _logger.LogWarning("No hay destinatarios configurados para el correo de avance por hora.");
            return;
        }

        var entradasValidas = reportes?
            .Where(r => r is not null)
            .ToList()
            ?? new List<ReporteAvanceHoraEmailEntry>();

        if (entradasValidas.Count == 0)
        {
            _logger.LogWarning("No se proporcionaron datos válidos para el correo de avance por hora.");
            return;
        }

        var subject = BuildSubject(options, entradasValidas);
        var body = BuildBody(options, entradasValidas);

        using var message = new MailMessage
        {
            From = new MailAddress(options.From, string.IsNullOrWhiteSpace(options.DisplayName) ? options.From : options.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        foreach (var destinatario in options.To.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            message.To.Add(destinatario);
        }

        if (message.To.Count == 0)
        {
            _logger.LogWarning("No se encontraron destinatarios válidos para enviar el reporte de avance por hora.");
            return;
        }

        using var smtp = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.UseSsl,
            Credentials = new NetworkCredential(options.From, options.Password)
        };

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(message);
            _logger.LogInformation("Reporte de avance por hora enviado correctamente para {Almacenes}.", ObtenerDescripcionLog(entradasValidas));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al enviar el correo de avance por hora para {Almacenes}.", ObtenerDescripcionLog(entradasValidas));
        }
    }

    private static string BuildSubject(ReporteAvanceHoraEmailOptions options, IReadOnlyCollection<ReporteAvanceHoraEmailEntry> reportes)
    {
        var baseSubject = string.IsNullOrWhiteSpace(options.Subject)
            ? "Reporte de avance por hora"
            : options.Subject!;

        if (reportes.Count == 0)
        {
            return baseSubject;
        }

        var horaFin = reportes
            .Select(r => r.Reporte.HoraFin)
            .Where(h => h != default)
            .OrderByDescending(h => h)
            .FirstOrDefault();

        if (horaFin == default)
        {
            horaFin = DateTime.Now;
        }

        var etiquetaAlmacen = reportes.Count == 1
            ? ObtenerEtiquetaAlmacen(reportes.First())
            : "Todos los almacenes";

        return $"{baseSubject} - {etiquetaAlmacen} - {horaFin:dd/MM/yyyy HH:mm}";
    }

    private static string BuildBody(ReporteAvanceHoraEmailOptions options, IReadOnlyCollection<ReporteAvanceHoraEmailEntry> reportes)
    {
        var culture = new CultureInfo("es-PE");
        var sb = new StringBuilder();

        var entradas = reportes
            .Where(r => r is not null)
            .ToList();

        for (var index = 0; index < entradas.Count; index++)
        {
            var entrada = entradas[index];
            var etiquetaAlmacen = ObtenerEtiquetaAlmacen(entrada);

            sb.AppendLine($"Almacén: {etiquetaAlmacen}");
            sb.AppendLine($"Avance en la hora: S/ {entrada.Reporte.TotalVentas.ToString("N2", culture)}");
            sb.AppendLine($"Avance acumulado del día: S/ {entrada.Reporte.TotalVentasDia.ToString("N2", culture)}");
            sb.AppendLine($"Tickets en la hora: {entrada.Reporte.Tickets}");
            sb.AppendLine($"Tickets acumulados del día: {entrada.Reporte.TicketsDia}");

            if (index < entradas.Count - 1)
            {
                sb.AppendLine();
                sb.AppendLine(new string('=', 27));
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrWhiteSpace(options.AdditionalMessage))
        {
            if (entradas.Count > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine(options.AdditionalMessage);
        }

        sb.AppendLine();
        sb.AppendLine("Este correo fue enviado automáticamente por GSCommerce.");

        return sb.ToString();
    }

    private static string ObtenerEtiquetaAlmacen(ReporteAvanceHoraEmailEntry entrada)
    {
        if (!string.IsNullOrWhiteSpace(entrada.NombreAlmacen))
        {
            return entrada.NombreAlmacen!;
        }

        return entrada.IdAlmacen.HasValue
            ? $"Almacén {entrada.IdAlmacen.Value}"
            : "Todos los almacenes";
    }

    private static string ObtenerDescripcionLog(IEnumerable<ReporteAvanceHoraEmailEntry> entradas)
    {
        var etiquetas = entradas
            .Where(e => e is not null)
            .Select(ObtenerEtiquetaAlmacen)
            .Distinct()
            .ToList();

        return etiquetas.Count switch
        {
            0 => "sin almacenes",
            1 => etiquetas[0],
            _ => "todos los almacenes"
        };
    }
}