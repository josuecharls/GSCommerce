using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Comprobante
{
    public int IdFe { get; set; }

    public int IdComprobante { get; set; }

    public string Hash { get; set; } = null!;

    public byte[]? Qr { get; set; }

    public bool? EnviadoSunat { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public string? TicketSunat { get; set; }

    public string? RespuestaSunat { get; set; }

    public DateTime? FechaRespuestaSunat { get; set; }

    public bool Estado { get; set; }

    public string? Xml { get; set; }

    public bool EsNota { get; set; }
}
