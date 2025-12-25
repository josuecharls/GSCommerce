using System;

namespace GSCommerceAPI.Models;

public partial class GuiaRemisionSunat
{
    public int IdGuiaRemisionSunat { get; set; }

    public int IdGuiaRemision { get; set; }

    public string? Hash { get; set; }

    public string? Xml { get; set; }

    public bool? EnviadoSunat { get; set; }

    public DateTime? FechaEnvio { get; set; }

    public string? TicketSunat { get; set; }

    public string? RespuestaSunat { get; set; }

    public DateTime? FechaRespuestaSunat { get; set; }

    public string? EstadoSunat { get; set; }

    public string? CodigoRespuestaSunat { get; set; }

    public string? MensajeRespuestaSunat { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public virtual GuiaRemisionCabecera IdGuiaRemisionNavigation { get; set; } = null!;
}


