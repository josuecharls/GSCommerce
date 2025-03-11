using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VDocumentosEnviadosSunat
{
    public string Tienda { get; set; } = null!;

    public string Hash { get; set; } = null!;

    public string? DocInicio { get; set; }

    public string? DocFin { get; set; }

    public int CantidadDocumentos { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public DateTime? FechaReferencia { get; set; }

    public string? TicketSunat { get; set; }

    public string? RespuestaSunat { get; set; }
}
