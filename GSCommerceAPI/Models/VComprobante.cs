using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class VComprobante
{
    public int IdFe { get; set; }

    public string Tienda { get; set; } = null!;

    public string TipoDoc { get; set; } = null!;

    public string? Numero { get; set; }

    public string? Fecha { get; set; }

    public decimal? Apagar { get; set; }

    public string Hash { get; set; } = null!;

    public bool EnviadoSunat { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public DateTime? FechaRespuestaSunat { get; set; }

    public string? RespuestaSunat { get; set; }

    public string? TicketSunat { get; set; }

    public string? Xml { get; set; }

    public int IdComprobante { get; set; }
}
