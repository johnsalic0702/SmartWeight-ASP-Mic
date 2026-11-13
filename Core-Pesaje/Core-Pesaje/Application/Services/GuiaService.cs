using System.Globalization;
using System.Text.Json;
using CorePesaje.Application.DTOs;
using CorePesaje.Infrastructure.Data.Repositories;

namespace CorePesaje.Application.Services;

public sealed class GuiaService(SqlProcedureRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    public async Task<object> SaveAsync(GuiaDTO dto, CancellationToken ct)
    {
        return await repository.ExecuteInTransactionAsync(async session =>
        {
            if (dto.detalle is null || dto.detalle.Count == 0)
            {
                var jsonWithoutDetails = JsonSerializer.Serialize(dto, JsonOptions);
                var result = await session.GuiaAsync(0, jsonWithoutDetails, checked((int)dto.guiaId), cancellationToken: ct);
                var row = SqlProcedureRepository.StatusRow(result) ?? new Dictionary<string, object?>();
                if (SqlProcedureRepository.IsFailedStatus(row)) session.MarkRollbackOnly();
                return (object)row;
            }

            var saved = new List<Dictionary<string, object?>>();
            foreach (var group in JavaGroupingOrder.ForLongKeys(dto.detalle.GroupBy(d => d.productoId)))
            {
                var grouped = new GuiaDTO
                {
                    guiaId = dto.guiaId, sucursalId = dto.sucursalId, almacenId = dto.almacenId,
                    documentoId = dto.documentoId, serieId = dto.serieId, clienteId = dto.clienteId,
                    cliente = dto.cliente, numero = dto.numero, fecha = dto.fecha, movimientoId = dto.movimientoId,
                    estado = dto.estado, glosa = dto.glosa, monedaId = dto.monedaId, tipoCambio = dto.tipoCambio,
                    vendedorId = dto.vendedorId, ptoPartida = dto.ptoPartida, ptoLlegada = dto.ptoLlegada,
                    transportistaId = dto.transportistaId, vehiculoId = dto.vehiculoId, conductorId = dto.conductorId,
                    motivoId = dto.motivoId, motivo = dto.motivo, pesoTara = dto.pesoTara,
                    pesoBruto = dto.pesoBruto, empresaId = dto.empresaId, usuarioId = dto.usuarioId,
                    detalle = group.ToList()
                };
                var json = JsonSerializer.Serialize(grouped, JsonOptions);
                var result = await session.GuiaAsync(0, json, checked((int)dto.guiaId), cancellationToken: ct);
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
            var result = await repository.GuiaAsync(tipo, null, checked((int)id), parsedDate, estado, 1, 1, 1, ct);
            return SqlProcedureRepository.ConcatenateFirstColumn(result);
        }
        catch { return "[]"; }
    }

    public Task<List<Dictionary<string, object?>>> GetComboAsync(long empresaId, long sucursalId,
        long almacenId, string fecha, CancellationToken ct) => repository.GuiasComboAsync(
            checked((int)empresaId), checked((int)sucursalId), checked((int)almacenId),
            DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture), ct);

    public async Task UpdateStateAsync(long id, int estado, long usuarioId, CancellationToken ct)
    {
        await repository.ActualizarEstadoAsync(2, id, estado, usuarioId, ct);
    }
}
