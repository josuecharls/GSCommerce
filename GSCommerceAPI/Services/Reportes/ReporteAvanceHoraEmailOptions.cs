using System.Collections.Generic;

namespace GSCommerceAPI.Services;

public class ReporteAvanceHoraEmailOptions
{
    public const string SectionName = "ReporteAvanceHoraEmail";

    public bool Enabled { get; set; }

    public string? From { get; set; }

    public string? Password { get; set; }

    public string? DisplayName { get; set; }

    public List<string> To { get; set; } = new();

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    public int SendStartHour { get; set; } = 9;

    public int SendEndHour { get; set; } = 22;

    public List<int> WarehouseIds { get; set; } = new();

    public string? Subject { get; set; }

    public string? AdditionalMessage { get; set; }
}