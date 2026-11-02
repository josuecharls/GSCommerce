using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using GSCommerceAPI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.Reportes;

public interface IAproximacionGastosEmailService
{
    Task EnviarAsync(AproximacionGastosReporte reporte, CancellationToken cancellationToken = default);
}

public class AproximacionGastosEmailService : IAproximacionGastosEmailService
{
    private static readonly (string Nombre, decimal PctMin, decimal PctMax)[] RangosGastos =
    {
        ("Sueldos", 0.20m, 0.25m),
        ("Materia Prima", 0.30m, 0.35m),
        ("Publicidad", 0.10m, 0.15m),
        ("Renta del local", 0.08m, 0.12m),
        ("Gastos del negocio", 0.05m, 0.08m)
    };

    private readonly ILogger<AproximacionGastosEmailService> _logger;
    private readonly IOptionsMonitor<AproximacionGastosEmailOptions> _optionsMonitor;
    private readonly IOptionsMonitor<ReporteAvanceHoraEmailOptions> _avanceOptionsMonitor;

    public AproximacionGastosEmailService(
        ILogger<AproximacionGastosEmailService> logger,
        IOptionsMonitor<AproximacionGastosEmailOptions> optionsMonitor,
        IOptionsMonitor<ReporteAvanceHoraEmailOptions> avanceOptionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _avanceOptionsMonitor = avanceOptionsMonitor;
    }

    public async Task EnviarAsync(AproximacionGastosReporte reporte, CancellationToken cancellationToken = default)
    {
        var options = ResolverOpciones();

        if (!options.Enabled)
        {
            _logger.LogDebug("El envío de aproximación de gastos por correo está deshabilitado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.From) || string.IsNullOrWhiteSpace(options.Password))
        {
            _logger.LogWarning("Las credenciales del remitente no están configuradas para AproximacionGastosEmail.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            _logger.LogWarning("El servidor SMTP no está configurado para AproximacionGastosEmail.");
            return;
        }

        if (options.To == null || options.To.Count == 0)
        {
            _logger.LogWarning("No hay destinatarios configurados para AproximacionGastosEmail.");
            return;
        }

        if (reporte.Filas.Count == 0)
        {
            _logger.LogInformation(
                "No se envía aproximación de gastos: sin tiendas con ventas del {Desde:yyyy-MM-dd} al {Hasta:yyyy-MM-dd}.",
                reporte.Desde,
                reporte.Hasta);
            return;
        }

        var subject = string.IsNullOrWhiteSpace(options.Subject)
            ? $"Aproximación de gastos vs ventas mensuales - {reporte.Hasta:dd/MM/yyyy}"
            : $"{options.Subject} - {reporte.Hasta:dd/MM/yyyy}";

        var body = BuildHtmlBody(reporte);

        using var message = new MailMessage
        {
            From = new MailAddress(options.From, string.IsNullOrWhiteSpace(options.DisplayName) ? options.From : options.DisplayName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        foreach (var destinatario in options.To.Where(d => !string.IsNullOrWhiteSpace(d)))
            message.To.Add(destinatario);

        if (message.To.Count == 0)
        {
            _logger.LogWarning("No se encontraron destinatarios válidos para AproximacionGastosEmail.");
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
            await smtp.SendMailAsync(message, cancellationToken);
            _logger.LogInformation(
                "Aproximación de gastos enviada correctamente ({Tiendas} tiendas, {Desde:yyyy-MM-dd} a {Hasta:yyyy-MM-dd}).",
                reporte.Filas.Count,
                reporte.Desde,
                reporte.Hasta);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Error al enviar aproximación de gastos del {Desde:yyyy-MM-dd} al {Hasta:yyyy-MM-dd}.",
                reporte.Desde,
                reporte.Hasta);
            throw;
        }
    }

    private static string BuildHtmlBody(AproximacionGastosReporte reporte)
    {
        var culture = new CultureInfo("es-PE");
        var sb = new StringBuilder();
        var periodo = $"{reporte.Desde.ToString("dd/MM/yyyy", culture)} - {reporte.Hasta.ToString("dd/MM/yyyy", culture)}";

        sb.AppendLine("<html><body style=\"font-family:Arial,sans-serif;font-size:13px;\">");
        sb.AppendLine($"<h2 style=\"margin:0 0 8px 0;\">Aproximación de gastos vs ventas mensuales</h2>");
        sb.AppendLine($"<p style=\"margin:0 0 16px 0;color:#555;\">Periodo: <strong>{periodo}</strong> (lo que va del mes)</p>");
        sb.AppendLine("<p style=\"margin:0 0 16px 0;color:#666;font-size:12px;\">Los rangos de gastos son una aproximación orientativa sobre las ventas. La utilidad proviene del reporte de utilidad por tiendas.</p>");

        foreach (var fila in reporte.Filas)
        {
            sb.AppendLine(BuildBloqueTienda(fila.Tienda, fila.Venta, fila.Utilidad, fila.Reinvertir, culture));
            sb.AppendLine("<br/>");
        }

        sb.AppendLine(BuildBloqueTienda(
            "TOTAL GENERAL",
            reporte.TotalVenta,
            reporte.TotalUtilidad,
            reporte.TotalReinvertir,
            culture,
            esTotal: true));

        sb.AppendLine("<p style=\"color:#666;font-size:11px;margin-top:16px;\">Correo enviado automáticamente por GSCommerce.</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private static string BuildBloqueTienda(
        string titulo,
        decimal venta,
        decimal utilidad,
        decimal reinvertir,
        CultureInfo culture,
        bool esTotal = false)
    {
        var sb = new StringBuilder();
        var bgHeader = esTotal ? "#FFFF00" : "#E8F0FE";

        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;width:100%;max-width:520px;\">");
        sb.AppendLine($"<tr style=\"background:{bgHeader};font-weight:bold;\"><td colspan=\"2\">{HtmlEncode(titulo)}</td></tr>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<td><strong>TOTAL DE VENTAS MENSUAL</strong></td>");
        sb.AppendLine($"<td style=\"text-align:right;\"><strong>{FormatearMonto(venta, culture)}</strong></td>");
        sb.AppendLine("</tr>");

        foreach (var (nombre, pctMin, pctMax) in RangosGastos)
        {
            var min = Math.Round(venta * pctMin, 2);
            var max = Math.Round(venta * pctMax, 2);
            var etiqueta = $"{nombre}: {(pctMin * 100):0}% - {(pctMax * 100):0}% de las ventas";
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HtmlEncode(etiqueta)}</td>");
            sb.AppendLine($"<td style=\"text-align:right;\">{FormatearMonto(min, culture)} - {FormatearMonto(max, culture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr>");
        sb.AppendLine("<td>Utilidad (reporte utilidad tiendas)</td>");
        sb.AppendLine($"<td style=\"text-align:right;\">{FormatearMonto(utilidad, culture)}</td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("<tr>");
        sb.AppendLine("<td>Reinvertir: 10% de la utilidad</td>");
        sb.AppendLine($"<td style=\"text-align:right;\">{FormatearMonto(reinvertir, culture)}</td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    private static string FormatearMonto(decimal monto, CultureInfo culture)
        => $"S/ {monto.ToString("N2", culture)}";

    private static string HtmlEncode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private AproximacionGastosEmailOptions ResolverOpciones()
    {
        var options = _optionsMonitor.CurrentValue;
        var avance = _avanceOptionsMonitor.CurrentValue;

        return new AproximacionGastosEmailOptions
        {
            Enabled = options.Enabled,
            From = string.IsNullOrWhiteSpace(options.From) ? avance.From : options.From,
            Password = string.IsNullOrWhiteSpace(options.Password) ? avance.Password : options.Password,
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? avance.DisplayName : options.DisplayName,
            To = options.To?.Count > 0 ? options.To : avance.To,
            SmtpHost = string.IsNullOrWhiteSpace(options.SmtpHost) ? avance.SmtpHost : options.SmtpHost,
            SmtpPort = options.SmtpPort > 0 ? options.SmtpPort : avance.SmtpPort,
            UseSsl = string.IsNullOrWhiteSpace(options.SmtpHost) ? avance.UseSsl : options.UseSsl,
            SendHour = options.SendHour,
            SendMinute = options.SendMinute,
            Subject = options.Subject
        };
    }
}
