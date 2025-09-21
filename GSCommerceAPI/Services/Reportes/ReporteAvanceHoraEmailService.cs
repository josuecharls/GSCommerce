using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using GSCommerce.Client.Models.DTOs.Reportes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services;

public interface IReporteAvanceHoraEmailService
{
    Task EnviarReporteAsync(ReporteAvanceHoraDTO reporte, string? nombreAlmacen, int? idAlmacen, CancellationToken cancellationToken = default);
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

    public async Task EnviarReporteAsync(ReporteAvanceHoraDTO reporte, string? nombreAlmacen, int? idAlmacen, CancellationToken cancellationToken = default)
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

        var subject = BuildSubject(options, reporte, nombreAlmacen, idAlmacen);
        var body = BuildBody(options, reporte, nombreAlmacen);

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
            _logger.LogInformation("Reporte de avance por hora enviado correctamente para {Almacen}.", nombreAlmacen ?? "todos los almacenes");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al enviar el correo de avance por hora para {Almacen}.", nombreAlmacen ?? "todos los almacenes");
        }
    }

    private static string BuildSubject(ReporteAvanceHoraEmailOptions options, ReporteAvanceHoraDTO reporte, string? nombreAlmacen, int? idAlmacen)
    {
        var baseSubject = string.IsNullOrWhiteSpace(options.Subject)
            ? "Reporte de avance por hora"
            : options.Subject!;

        var etiquetaAlmacen = !string.IsNullOrWhiteSpace(nombreAlmacen)
            ? nombreAlmacen
            : idAlmacen.HasValue ? $"Almacén {idAlmacen.Value}" : "Todos los almacenes";

        return $"{baseSubject} - {etiquetaAlmacen} - {reporte.HoraFin:dd/MM/yyyy HH:mm}";
    }

    private static string BuildBody(ReporteAvanceHoraEmailOptions options, ReporteAvanceHoraDTO reporte, string? nombreAlmacen)
    {
        var culture = new CultureInfo("es-PE");
        var sb = new StringBuilder();

        sb.AppendLine("GSCommerce - Reporte de avance por hora");
        if (!string.IsNullOrWhiteSpace(nombreAlmacen))
        {
            sb.AppendLine($"Almacén: {nombreAlmacen}");
        }
        sb.AppendLine($"Intervalo seleccionado: {reporte.HoraInicio:dd/MM/yyyy HH:mm} - {reporte.HoraFin:dd/MM/yyyy HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"Avance en la hora: S/ {reporte.TotalVentas.ToString("N2", culture)}");
        sb.AppendLine($"Avance acumulado del día: S/ {reporte.TotalVentasDia.ToString("N2", culture)}");
        sb.AppendLine($"Tickets en la hora: {reporte.Tickets}");
        sb.AppendLine($"Tickets acumulados del día: {reporte.TicketsDia}");

        if (!string.IsNullOrWhiteSpace(options.AdditionalMessage))
        {
            sb.AppendLine();
            sb.AppendLine(options.AdditionalMessage);
        }

        if (reporte.DetalleHoras?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Detalle por hora:");
            sb.AppendLine("Hora | Intervalo | Ventas hora | Ventas acumuladas | Tickets hora | Tickets acumulados");

            foreach (var detalle in reporte.DetalleHoras.OrderBy(d => d.HoraInicio))
            {
                sb.AppendLine(
                    $"{detalle.HoraInicio:HH:mm} | {detalle.HoraInicio:HH:mm}-{detalle.HoraFin:HH:mm} | " +
                    $"S/ {detalle.TotalHora.ToString("N2", culture)} | S/ {detalle.TotalAcumulado.ToString("N2", culture)} | " +
                    $"{detalle.TicketsHora} | {detalle.TicketsAcumulados}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Este correo fue enviado automáticamente por GSCommerce.");

        return sb.ToString();
    }
}