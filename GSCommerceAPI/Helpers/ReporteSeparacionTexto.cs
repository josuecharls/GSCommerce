using System.Text.RegularExpressions;

namespace GSCommerceAPI.Helpers;

public static class ReporteSeparacionTexto
{
    private static readonly Regex EspaciosMultiples = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Clave estable para comparar/agrupar textos de catálogo (marca, talla, color)
    /// pese a variaciones como "MVAR" vs "MVAR." o espacios extra.
    /// </summary>
    public static string NormalizarClave(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return string.Empty;
        }

        var t = s.Trim();
        t = EspaciosMultiples.Replace(t, " ");
        while (t.Length > 0 && EsPuntuacionFinal(t[^1]))
        {
            t = t[..^1].TrimEnd();
        }

        return t.ToUpperInvariant();
    }

    private static bool EsPuntuacionFinal(char c) => c is '.' or ',' or ';' or ':' or '-' or '_';

    public static bool EsDimensionConValor(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        return !string.Equals(s.Trim(), "NO APLICA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Etiqueta preferida dentro de un grupo de variantes (la más corta, luego alfabética).
    /// </summary>
    public static string EtiquetaCanonica(IEnumerable<string> variantes)
    {
        var lista = variantes
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .ToList();

        if (lista.Count == 0)
        {
            return string.Empty;
        }

        return lista
            .OrderBy(v => v.Length)
            .ThenBy(v => v, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
