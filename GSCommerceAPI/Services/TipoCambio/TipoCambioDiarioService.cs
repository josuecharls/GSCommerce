using System.Data;
using GSCommerce.Client.Models;
using GSCommerceAPI.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GSCommerceAPI.Services.TipoCambio;

public class TipoCambioDiarioService
{
    private readonly SyscharlesContext _context;

    public TipoCambioDiarioService(SyscharlesContext context)
    {
        _context = context;
    }

    public async Task<TipoCambioReplicacionResultadoDTO> ReplicarAsync(DateOnly? fecha = null, CancellationToken cancellationToken = default)
    {
        var fechaDestino = fecha ?? DateOnly.FromDateTime(DateTime.Today);
        var connectionString = _context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión para replicar el tipo de cambio.");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new SqlCommand("Maestros.usp_InsUpd_TipoCambioDiario", connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        command.Parameters.Add("@Fecha", SqlDbType.Date).Value = fechaDestino.ToDateTime(TimeOnly.MinValue);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("El SP Maestros.usp_InsUpd_TipoCambioDiario no devolvió resultado.");
        }

        return new TipoCambioReplicacionResultadoDTO
        {
            Accion = reader["Accion"]?.ToString() ?? string.Empty,
            FechaOrigen = reader["FechaOrigen"] is DateTime origen ? DateOnly.FromDateTime(origen) : null,
            FechaDestino = reader["FechaDestino"] is DateTime destino
                ? DateOnly.FromDateTime(destino)
                : fechaDestino,
            Compra = reader["Compra"] is decimal compra ? compra : null,
            Venta = reader["Venta"] is decimal venta ? venta : null
        };
    }
}
