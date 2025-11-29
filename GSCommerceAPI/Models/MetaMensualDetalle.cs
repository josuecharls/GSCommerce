using System;

namespace GSCommerceAPI.Models;

public partial class MetaMensualDetalle
{
    public int IdMetaMensualDetalle { get; set; }

    public int IdMetaMensual { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal MetaDiaria { get; set; }

    public decimal? VentaReal { get; set; }

    public decimal? PorcentajeCumplimiento { get; set; }

    public virtual MetaMensual IdMetaMensualNavigation { get; set; } = null!;
}

