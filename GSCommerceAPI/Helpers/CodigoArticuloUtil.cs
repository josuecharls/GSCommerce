namespace GSCommerceAPI.Helpers;

public static class CodigoArticuloUtil
{
    /// <summary>Normaliza códigos numéricos a 6 dígitos (ej. 5060 → 005060).</summary>
    public static string Normalizar(string? raw)
    {
        var s = (raw ?? string.Empty).Trim();
        if (s.Length == 0)
            return string.Empty;
        if (int.TryParse(s, System.Globalization.NumberStyles.Integer, null, out var n))
            return n.ToString("D6");
        return s.Length > 6 ? s : s.PadLeft(6, '0');
    }
}
