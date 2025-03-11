using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Bakfebolsinenvio
{
    public int IdComprobante { get; set; }

    public string Hash { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public string? TicketSunat { get; set; }

    public string? RespuestaSunat { get; set; }

    public DateTime? FechaRespuestaSunat { get; set; }

    public string? Xml { get; set; }
}
