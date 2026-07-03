using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GSCommerceAPI.Services.Reportes;

public interface ICuadroCajaEmailService
{
    Task EnviarCuadrosAsync(
        CuadroVentasEfectivoReporte ventasEfectivo,
        BilleteraVirtualReporte billeteraVirtual,
        CancellationToken cancellationToken = default);
}

public class CuadroCajaEmailService : ICuadroCajaEmailService
{
    private readonly ILogger<CuadroCajaEmailService> _logger;
    private readonly IOptionsMonitor<CuadroCajaEmailOptions> _optionsMonitor;
    private readonly IOptionsMonitor<ReporteAvanceHoraEmailOptions> _avanceOptionsMonitor;

    public CuadroCajaEmailService(
        ILogger<CuadroCajaEmailService> logger,
        IOptionsMonitor<CuadroCajaEmailOptions> optionsMonitor,
        IOptionsMonitor<ReporteAvanceHoraEmailOptions> avanceOptionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _avanceOptionsMonitor = avanceOptionsMonitor;
    }

    public async Task EnviarCuadrosAsync(
        CuadroVentasEfectivoReporte ventasEfectivo,
        BilleteraVirtualReporte billeteraVirtual,
        CancellationToken cancellationToken = default)
    {
        var options = ResolverOpciones();

        if (!options.Enabled)
        {
            _logger.LogDebug("El envío de cuadros de caja por correo está deshabilitado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.From) || string.IsNullOrWhiteSpace(options.Password))
        {
            _logger.LogWarning("Las credenciales del remitente no están configuradas para CuadroCajaEmail.");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            _logger.LogWarning("El servidor SMTP no está configurado para CuadroCajaEmail.");
            return;
        }

        if (options.To == null || options.To.Count == 0)
        {
            _logger.LogWarning("No hay destinatarios configurados para CuadroCajaEmail.");
            return;
        }

        var fecha = ventasEfectivo.Fecha;
        var subject = string.IsNullOrWhiteSpace(options.Subject)
            ? $"Cuadro de Ventas y Billetera Virtual - {fecha:dd/MM/yyyy}"
            : $"{options.Subject} - {fecha:dd/MM/yyyy}";

        var body = BuildHtmlBody(ventasEfectivo, billeteraVirtual);

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
            _logger.LogWarning("No se encontraron destinatarios válidos para CuadroCajaEmail.");
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
            _logger.LogInformation("Cuadros de caja enviados correctamente para {Fecha:yyyy-MM-dd}.", fecha);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al enviar los cuadros de caja para {Fecha:yyyy-MM-dd}.", fecha);
            throw;
        }
    }

    private static string BuildHtmlBody(CuadroVentasEfectivoReporte ventas, BilleteraVirtualReporte billetera)
    {
        var culture = new CultureInfo("es-PE");
        var sb = new StringBuilder();

        sb.AppendLine("<html><body style=\"font-family:Arial,sans-serif;font-size:13px;\">");

        if (ventas.Filas.Count > 0)
        {
            sb.AppendLine(BuildTablaVentasEfectivo(ventas, culture));
            sb.AppendLine("<br/>");
        }

        if (ventas.FilasDolares.Count > 0)
        {
            sb.AppendLine(BuildTablaVentasEfectivoDolares(ventas, culture));
            sb.AppendLine("<br/>");
        }

        sb.AppendLine(BuildTablaBilleteraVirtual(billetera, culture));

        sb.AppendLine("<p style=\"color:#666;font-size:11px;\">Correo enviado automáticamente por GSCommerce.</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private static string BuildTablaVentasEfectivo(CuadroVentasEfectivoReporte reporte, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var fecha = reporte.Fecha.ToString("dd/MM/yyyy", culture);

        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;text-align:center;\">");
        sb.AppendLine($"<tr style=\"background:#FFFF00;font-weight:bold;\"><td colspan=\"3\">CUADRO DE VENTAS Y EFECTIVO ({fecha})</td></tr>");
        sb.AppendLine("<tr style=\"font-weight:bold;\"><td>TIENDA</td><td>VENTA DEL DIA</td><td>EFECTIVO (SALDO DE CAJA)</td></tr>");

        foreach (var fila in reporte.Filas)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HtmlEncode(fila.Tienda)}</td>");
            sb.AppendLine($"<td>{FormatearMonto(fila.VentaDia, culture)}</td>");
            sb.AppendLine($"<td>{FormatearMonto(fila.EfectivoSaldoCaja, culture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr style=\"background:#FFFF00;font-weight:bold;\">");
        sb.AppendLine("<td>TOTAL</td>");
        sb.AppendLine($"<td>{FormatearMonto(reporte.TotalVentaDia, culture)}</td>");
        sb.AppendLine($"<td>{FormatearMonto(reporte.TotalEfectivo, culture)}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");

        return sb.ToString();
    }

    private static string BuildTablaVentasEfectivoDolares(CuadroVentasEfectivoReporte reporte, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var fecha = reporte.Fecha.ToString("dd/MM/yyyy", culture);

        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;text-align:center;\">");
        sb.AppendLine($"<tr style=\"background:#FFFF00;font-weight:bold;\"><td colspan=\"4\">CUADRO DE VENTAS Y EFECTIVO S/ | $ ({fecha})</td></tr>");
        sb.AppendLine("<tr style=\"font-weight:bold;\"><td>TIENDA</td><td>VENTA DEL DIA SOLES</td><td>VENTA DEL DIA DOLARES</td><td>EFECTIVO (SALDO DE CAJA)</td></tr>");

        foreach (var fila in reporte.FilasDolares)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{HtmlEncode(fila.Tienda)}</td>");
            sb.AppendLine($"<td>{FormatearMonto(fila.VentaDiaSoles, culture)}</td>");
            sb.AppendLine($"<td>{FormatearMontoDolares(fila.VentaDiaDolares, culture)}</td>");
            sb.AppendLine($"<td>{FormatearMonto(fila.EfectivoSaldoCaja, culture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("<tr style=\"background:#FFFF00;font-weight:bold;\">");
        sb.AppendLine("<td>TOTAL</td>");
        sb.AppendLine($"<td>{FormatearMonto(reporte.TotalVentaDiaSoles, culture)}</td>");
        sb.AppendLine($"<td>{FormatearMontoDolares(reporte.TotalVentaDiaDolares, culture)}</td>");
        sb.AppendLine($"<td>{FormatearMonto(reporte.TotalEfectivoDolares, culture)}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");

        return sb.ToString();
    }

    private static string BuildTablaBilleteraVirtual(BilleteraVirtualReporte reporte, CultureInfo culture)
    {
        var sb = new StringBuilder();
        var fecha = reporte.Fecha.ToString("dd/MM/yyyy", culture);
        var columnas = reporte.RazonesSociales;
        var colspan = columnas.Count + 2;

        sb.AppendLine("<table border=\"1\" cellpadding=\"6\" cellspacing=\"0\" style=\"border-collapse:collapse;text-align:center;\">");
        sb.AppendLine($"<tr style=\"background:#FFFF00;font-weight:bold;\"><td colspan=\"{colspan}\">BILLETERA VIRTUAL ({fecha})</td></tr>");

        sb.Append("<tr style=\"font-weight:bold;\"><td>TIPO</td>");
        foreach (var razon in columnas)
            sb.Append($"<td>{HtmlEncode(razon)}</td>");
        sb.AppendLine("<td>TOTAL</td></tr>");

        foreach (var fila in reporte.Filas)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{HtmlEncode(fila.Tipo)}</td>");
            foreach (var razon in columnas)
            {
                var monto = fila.MontosPorRazon.TryGetValue(razon, out var valor) ? valor : 0m;
                sb.Append($"<td>{FormatearMonto(monto, culture)}</td>");
            }
            sb.Append($"<td>{FormatearMonto(fila.Total, culture)}</td>");
            sb.AppendLine("</tr>");
        }

        sb.Append("<tr style=\"background:#FFFF00;font-weight:bold;\"><td>TOTAL</td>");
        foreach (var razon in columnas)
        {
            var monto = reporte.TotalesPorRazon.TryGetValue(razon, out var valor) ? valor : 0m;
            sb.Append($"<td>{FormatearMonto(monto, culture)}</td>");
        }
        sb.Append($"<td>{FormatearMonto(reporte.TotalGeneral, culture)}</td>");
        sb.AppendLine("</tr>");
        sb.AppendLine("</table>");

        return sb.ToString();
    }

    private static string FormatearMonto(decimal monto, CultureInfo culture)
        => $"S/ {monto.ToString("N2", culture)}";

    private static string FormatearMontoDolares(decimal monto, CultureInfo culture)
        => $"$ {monto.ToString("N2", culture)}";

    private static string HtmlEncode(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);

    private CuadroCajaEmailOptions ResolverOpciones()
    {
        var options = _optionsMonitor.CurrentValue;
        var avance = _avanceOptionsMonitor.CurrentValue;

        return new CuadroCajaEmailOptions
        {
            Enabled = options.Enabled,
            From = string.IsNullOrWhiteSpace(options.From) ? avance.From : options.From,
            Password = string.IsNullOrWhiteSpace(options.Password) ? avance.Password : options.Password,
            DisplayName = string.IsNullOrWhiteSpace(options.DisplayName) ? avance.DisplayName : options.DisplayName,
            To = options.To?.Count > 0 ? options.To : avance.To,
            SmtpHost = string.IsNullOrWhiteSpace(options.SmtpHost) ? avance.SmtpHost : options.SmtpHost,
            SmtpPort = options.SmtpPort > 0 ? options.SmtpPort : avance.SmtpPort,
            UseSsl = string.IsNullOrWhiteSpace(options.SmtpHost) ? avance.UseSsl : options.UseSsl,
            Subject = options.Subject
        };
    }
}
