using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class TipoDocumentoVentum
{
    public int IdTipoDocumentoVenta { get; set; }

    public string Descripcion { get; set; } = null!;

    public string Abreviatura { get; set; } = null!;

    public bool Manual { get; set; }

    public bool Estado { get; set; }

    public virtual ICollection<ComprobanteDeVentaCabecera> ComprobanteDeVentaCabeceras { get; set; } = new List<ComprobanteDeVentaCabecera>();

    public virtual ICollection<NotaDeCreditoCabecera> NotaDeCreditoCabeceras { get; set; } = new List<NotaDeCreditoCabecera>();

    public virtual ICollection<SerieCorrelativo> SerieCorrelativos { get; set; } = new List<SerieCorrelativo>();
}
