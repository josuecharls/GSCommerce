using System.Collections.Generic;

namespace GSCommerceAPI.Models;

public partial class StockAlmacenRepuesto
{
    public int IdStockAlmacenRepuesto { get; set; }

    public int IdAlmacenRepuesto { get; set; }

    public string IdArticulo { get; set; } = null!;

    public int Cantidad { get; set; }

    public virtual AlmacenRepuesto IdAlmacenRepuestoNavigation { get; set; } = null!;

    public virtual Articulo IdArticuloNavigation { get; set; } = null!;
}
