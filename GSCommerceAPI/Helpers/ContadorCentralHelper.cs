namespace GSCommerceAPI.Helpers;

/// <summary>
/// Usuario administrador centralizado (contador) que emite notas de crédito
/// para cualquier almacén sin depender de su almacén de origen.
/// </summary>
public static class ContadorCentralHelper
{
    public const int IdUsuarioContadorCentral = 4060;

    public static bool EsContadorCentral(int userId) =>
        userId == IdUsuarioContadorCentral;

    public static bool EsContadorCentral(int? userId) =>
        userId.HasValue && EsContadorCentral(userId.Value);

    public static bool PuedeEmitirNcMultiAlmacen(int userId) =>
        EsContadorCentral(userId);
}
