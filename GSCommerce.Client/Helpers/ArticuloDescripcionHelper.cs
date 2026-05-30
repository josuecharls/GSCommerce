using GSCommerce.Client.Models;

namespace GSCommerce.Client.Helpers;

public static class ArticuloDescripcionHelper
{
    /// <summary>
    /// Arma Descripcion y DescripcionCorta desde complementos.
    /// Artículos antiguos sin variantes: incluye Color/Talla de la cabecera Articulos.
    /// Con variantes: omite Color/Talla de cabecera; si hay una sola variante, agrega la suya.
    /// </summary>
    public static void AplicarDescripciones(ArticuloDTO articulo, IReadOnlyList<ArticuloVarianteDTO>? variantes = null)
    {
        var tieneVariantes = variantes is { Count: > 0 };
        var incluirColorTallaCabecera = !tieneVariantes;

        var descripcion = ConstruirDescripcionLarga(articulo, incluirColorTallaCabecera);
        var descripcionCorta = ConstruirDescripcionCorta(articulo, incluirColorTallaCabecera);

        if (tieneVariantes)
        {
            descripcion = QuitarColorTallaLegacy(descripcion, articulo);
            descripcionCorta = QuitarColorTallaLegacy(descripcionCorta, articulo);
        }

        if (variantes?.Count == 1)
        {
            var v = variantes[0];
            var sufijoVariante = UnirPartes(new[] { v.Color, v.Talla });
            if (!string.IsNullOrEmpty(sufijoVariante))
            {
                descripcion = $"{descripcion} {sufijoVariante}".Trim();
                descripcionCorta = $"{descripcionCorta} {sufijoVariante}".Trim();
            }
        }

        articulo.Descripcion = descripcion;
        articulo.DescripcionCorta = descripcionCorta;
    }

    private static string ConstruirDescripcionLarga(ArticuloDTO articulo, bool incluirColorTallaCabecera)
    {
        var partes = new List<string?>
        {
            articulo.Familia,
            articulo.Linea,
            articulo.Marca,
            articulo.Material,
            articulo.Modelo
        };

        if (incluirColorTallaCabecera)
            partes.Add(articulo.Color);

        partes.Add(articulo.Detalle);

        if (incluirColorTallaCabecera)
            partes.Add(articulo.Talla);

        return UnirPartes(partes);
    }

    private static string ConstruirDescripcionCorta(ArticuloDTO articulo, bool incluirColorTallaCabecera)
    {
        var partes = new List<string?>
        {
            articulo.Familia,
            articulo.Marca,
            articulo.Modelo
        };

        if (incluirColorTallaCabecera)
            partes.Add(articulo.Color);

        partes.Add(articulo.Detalle);

        if (incluirColorTallaCabecera)
            partes.Add(articulo.Talla);

        return UnirPartes(partes);
    }

    private static string UnirPartes(IEnumerable<string?> partes) =>
        string.Join(" ", partes.Where(ParteValida)).Trim();

    /// <summary>Omite vacíos y el marcador "No Aplica" de complementos.</summary>
    private static bool ParteValida(string? parte) =>
        !string.IsNullOrWhiteSpace(parte) &&
        !parte.Equals("No Aplica", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Por si Color/Talla legacy quedaron al final del texto (p. ej. descripción ya guardada en BD).
    /// </summary>
    private static string QuitarColorTallaLegacy(string texto, ArticuloDTO articulo)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return texto;

        var result = texto.Trim();

        if (ParteValida(articulo.Talla) &&
            result.EndsWith(articulo.Talla!, StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^articulo.Talla!.Length].TrimEnd();
        }

        if (ParteValida(articulo.Color) &&
            result.EndsWith(articulo.Color, StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^articulo.Color.Length].TrimEnd();
        }

        return result.Trim();
    }
}
