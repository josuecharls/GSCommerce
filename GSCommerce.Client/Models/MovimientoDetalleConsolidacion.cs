namespace GSCommerce.Client.Models;

/// <summary>
/// Une líneas con el mismo código (y variante) sumando cantidades,
/// para que el stock se valide y mueva una sola vez por artículo.
/// </summary>
public static class MovimientoDetalleConsolidacion
{
    public static List<MovimientoDetalleDTO> ConsolidarPorCodigo(IEnumerable<MovimientoDetalleDTO>? detalles)
    {
        if (detalles == null)
            return new List<MovimientoDetalleDTO>();

        return detalles
            .Where(d => !string.IsNullOrWhiteSpace(d.IdArticulo) && d.Cantidad > 0)
            .GroupBy(d => (
                Articulo: d.IdArticulo.Trim(),
                Variante: d.IdVariante is > 0 ? d.IdVariante : null))
            .Select((g, i) =>
            {
                var first = g.First();
                return new MovimientoDetalleDTO
                {
                    Item = i + 1,
                    IdArticulo = g.Key.Articulo,
                    IdVariante = g.Key.Variante,
                    DescripcionArticulo = first.DescripcionArticulo,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Valor = first.Valor
                };
            })
            .ToList();
    }
}
