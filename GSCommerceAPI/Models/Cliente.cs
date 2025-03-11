using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string TipoDocumento { get; set; } = null!;

    public string Dniruc { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Dpd { get; set; }

    public string? Telefono { get; set; }

    public string? Celular { get; set; }

    public string? Email { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<ComprobanteDeVentaCabecera> ComprobanteDeVentaCabeceras { get; set; } = new List<ComprobanteDeVentaCabecera>();

    public virtual ICollection<NotaDeCreditoCabecera> NotaDeCreditoCabeceras { get; set; } = new List<NotaDeCreditoCabecera>();
}
