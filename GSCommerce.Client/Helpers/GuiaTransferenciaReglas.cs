using GSCommerce.Client.Models;

namespace GSCommerce.Client.Helpers;

public static class GuiaTransferenciaReglas
{
    public static readonly TimeSpan PlazoAnulacionTrasConfirmar = TimeSpan.FromHours(72);

    public static bool EsTransferencia(MovimientoGuiaDTO? g)
        => g?.Tipo?.Equals("T", StringComparison.OrdinalIgnoreCase) == true
           && (EsEgreso(g) || EsIngreso(g));

    public static bool EsEgreso(MovimientoGuiaDTO? g)
        => string.Equals(g?.Motivo?.Trim(), "TRANSFERENCIA EGRESO", StringComparison.OrdinalIgnoreCase);

    public static bool EsIngreso(MovimientoGuiaDTO? g)
        => string.Equals(g?.Motivo?.Trim(), "TRANSFERENCIA INGRESO", StringComparison.OrdinalIgnoreCase);

    public static bool EstaAnulada(MovimientoGuiaDTO? g)
        => string.Equals(g?.Estado?.Trim(), "A", StringComparison.OrdinalIgnoreCase);

    public static bool EstaConfirmada(MovimientoGuiaDTO? g)
        => string.Equals(g?.Estado?.Trim(), "C", StringComparison.OrdinalIgnoreCase)
           || g?.IdUsuarioConfirma.HasValue == true;

    public static bool PuedeEditar(MovimientoGuiaDTO? g)
        => g != null && !EstaAnulada(g) && !EstaConfirmada(g);

    public static bool DentroDePlazoAnulacion(DateTime? fechaHoraConfirma, DateTime ahora)
        => fechaHoraConfirma.HasValue
           && ahora - fechaHoraConfirma.Value <= PlazoAnulacionTrasConfirmar;

    /// <summary>
    /// Transferencias: Anular solo en EGRESO antes de confirmar, o INGRESO confirmado dentro de 72 h.
    /// Ingresos/egresos directos: se muestra igual que antes (se deshabilita si ya está anulada).
    /// </summary>
    public static bool MostrarBotonAnular(MovimientoGuiaDTO? g, DateTime ahora)
    {
        if (g == null || EstaAnulada(g))
            return !EsTransferencia(g) && g != null;

        if (!EsTransferencia(g))
            return true;

        return PuedeAnular(g, ahora);
    }

    public static bool PuedeAnular(MovimientoGuiaDTO? g, DateTime ahora)
    {
        if (g == null || EstaAnulada(g))
            return false;

        if (!EsTransferencia(g))
            return true;

        if (EsEgreso(g))
            return !EstaConfirmada(g);

        if (EsIngreso(g))
            return EstaConfirmada(g) && DentroDePlazoAnulacion(g.FechaHoraConfirma, ahora);

        return false;
    }
}
