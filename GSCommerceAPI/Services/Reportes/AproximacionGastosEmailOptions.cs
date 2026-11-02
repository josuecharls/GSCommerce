using System.Collections.Generic;

namespace GSCommerceAPI.Services.Reportes;

public class AproximacionGastosEmailOptions
{
    public const string SectionName = "AproximacionGastosEmail";

    public bool Enabled { get; set; }

    public string? From { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }

    public List<string> To { get; set; } = new();

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public int SendHour { get; set; } = 23;

    public int SendMinute { get; set; } = 0;

    public string? Subject { get; set; }
}
