using System;
using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class GuiaRemisionDetalle
{
    public int IdGuiaRemision { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = null!;

    public string DescripcionArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public string UnidadMedida { get; set; } = null!;

    public virtual GuiaRemisionCabecera IdGuiaRemisionNavigation { get; set; } = null!;
}
