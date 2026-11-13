using System.Globalization;
using System.Text.Json;
using CorePesaje.Application.DTOs;
using CorePesaje.Infrastructure.Data.Repositories;

namespace CorePesaje.Application.Services;

public sealed class PedidoService(SqlProcedureRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    public async Task<object> SaveAsync(PedidoDTO dto, CancellationToken ct)
    {
        return await repository.ExecuteInTransactionAsync(async session =>
        {
            if (dto.detalle is null || dto.detalle.Count == 0)
            {
                var json = JsonSerializer.Serialize(dto, JsonOptions);
                var result = await session.PedidoAsync(0, json, checked((int)dto.pedidoId), estado: null, cancellationToken: ct);
                var row = SqlProcedureRepository.StatusRow(result) ?? new Dictionary<string, object?>();
                if (SqlProcedureRepository.IsFailedStatus(row)) session.MarkRollbackOnly();
                return (object)row;
            }

            var saved = new List<Dictionary<string, object?>>();
            foreach (var group in JavaGroupingOrder.ForLongKeys(dto.detalle.GroupBy(d => d.clienteId)))
            {
                var grouped = new PedidoDTO
                {
                    pedidoId = dto.pedidoId, clienteId = group.Key, fecha = dto.fecha,
                    cantidad = dto.cantidad, orden = dto.orden, estado = dto.estado,
                    obs = dto.obs, usuarioId = dto.usuarioId, detalle = group.ToList()
                };
                var json = JsonSerializer.Serialize(grouped, JsonOptions);
                var result = await session.PedidoAsync(0, json, checked((int)dto.pedidoId), estado: null, cancellationToken: ct);
                var row = SqlProcedureRepository.StatusRow(result);
                if (row is not null) saved.Add(row);
                if (SqlProcedureRepository.IsFailedStatus(row))
                {
                    session.MarkRollbackOnly();
                    return row!;
                }
            }
            return saved.Count == 0 ? new Dictionary<string, object?>() : saved[0];
        }, ct);
    }

    public async Task<string> GetJsonAsync(int tipo, long id, string? fecha, int estado, CancellationToken ct)
    {
        try
        {
            DateOnly? parsedDate = fecha is null ? null : DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var result = await repository.PedidoAsync(tipo, null, checked((int)id), parsedDate, estado, 1, ct);
            return SqlProcedureRepository.ConcatenateFirstColumn(result);
        }
        catch { return "[]"; }
    }

    public async Task UpdateStateAsync(long id, int estado, long usuarioId, CancellationToken ct)
    {
        await repository.ActualizarEstadoAsync(4, id, estado, usuarioId, ct);
    }
}
