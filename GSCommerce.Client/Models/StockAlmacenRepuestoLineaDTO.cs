namespace GSCommerce.Client.Models;

public class StockAlmacenRepuestoLineaDTO
{
    public int IdStockAlmacenRepuesto { get; set; }

    public int IdAlmacenRepuesto { get; set; }

    public string IdArticulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public int Cantidad { get; set; }
}
