namespace GSCommerce.Client.Models;

/// <summary>Ingreso (I) o retiro (E) de prendas en un sótano. El stock es independiente del de la tienda.</summary>
public class AjusteStockRepuestoRequest
{
    public int IdAlmacenRepuesto { get; set; }

    public string IdArticulo { get; set; } = string.Empty;

    /// <summary>I = ingreso al sótano, E = salida del sótano (reducción).</summary>
    public string Tipo { get; set; } = "I";

    public int Cantidad { get; set; }
}
