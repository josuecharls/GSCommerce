namespace GSCommerce.Client.Helpers;

/// <summary>
/// Usuario administrador centralizado (contador) con acceso restringido
/// y permiso para emitir notas de crédito en cualquier almacén.
/// </summary>
public static class ContadorCentralHelper
{
    public const int IdUsuarioContadorCentral = 4060;

    private static readonly string[] RutasExactas =
    {
        "/ventas",
        "/notass-credito",
        "/comprobantes-sunat",
        "/guias-remision-sunat",
        "/series-guias-remision",
        "/kardex-detallado",
        "/cambiar-password"
    };

    private static readonly string[] PrefijosPermitidos =
    {
        "/nota-credito/",
        "/guias-remision-sunat/"
    };

    public static bool EsContadorCentral(int? userId) =>
        userId.HasValue && userId.Value == IdUsuarioContadorCentral;

    public static bool EsRutaPermitida(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return true;

        var normalized = path.Split('?', '#')[0].TrimEnd('/');
        if (normalized.Length == 0 || string.Equals(normalized, "/", StringComparison.Ordinal))
            return true;

        foreach (var ruta in RutasExactas)
        {
            if (string.Equals(normalized, ruta, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var prefijo in PrefijosPermitidos)
        {
            if (normalized.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
