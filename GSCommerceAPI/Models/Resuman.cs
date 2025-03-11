using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Resuman
{
    public int IdResumenFe { get; set; }

    public int Correlativo { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string Hash { get; set; } = null!;

    public string DocInicio { get; set; } = null!;

    public string DocFin { get; set; } = null!;

    public int CantidadDocumentos { get; set; }

    public DateTime FechaEnvio { get; set; }

    public DateTime? FechaReferencia { get; set; }

    public bool EnvioSunat { get; set; }

    public string TicketSunat { get; set; } = null!;

    public string RespuestaSunat { get; set; } = null!;

    public DateTime FechaRespuestaSunat { get; set; }

    public string Tienda { get; set; } = null!;
}
