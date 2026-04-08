using System.Globalization;
using GSCommerce.Client.Models;

namespace GSCommerce.Client;

internal static class BarcodePrintMetadataQuery
{
    /// <summary>
    /// Query string sin '?'. El precio no va en la ruta: un segmento como "55.00" suele tratarse como
    /// archivo (extensión ".00") y el host devuelve 404 sin cargar la SPA.
    /// </summary>
    public static string BuildPrintQuery(
        string descripcion,
        ArticuloDTO articulo,
        decimal precioVenta,
        string? color = null,
        string? talla = null)
    {
        var parts = new List<string>
        {
            $"precio={Uri.EscapeDataString(precioVenta.ToString(CultureInfo.InvariantCulture))}"
        };

        if (!string.IsNullOrWhiteSpace(descripcion))
            parts.Add($"descripcion={Uri.EscapeDataString(descripcion.Trim())}");

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                parts.Add($"{key}={Uri.EscapeDataString(value.Trim())}");
        }

        Add("linea", articulo.Linea);
        Add("marca", articulo.Marca);
        Add("familia", articulo.Familia);
        Add("material", articulo.Material);
        Add("modelo", articulo.Modelo);
        Add("fc", articulo.FechaRegistro.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        Add("color", color);
        Add("talla", talla);

        return string.Join("&", parts);
    }
}
