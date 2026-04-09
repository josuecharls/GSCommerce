namespace GSCommerceAPI.Models.DTOs;

/// <summary>
/// Columnas alineadas con <c>Almacen.MovimientosDetalleType</c> para el TVP del SP.
/// </summary>
public sealed class MovimientosDetalleTvpDTO
{
    public int IdMovimiento { get; set; }

    public int Item { get; set; }

    public string IdArticulo { get; set; } = string.Empty;

    public string DescripcionArticulo { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal Valor { get; set; }
}
