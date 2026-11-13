using System.Globalization;
using System.Text.Json;
using CorePesaje.Application.DTOs;
using CorePesaje.Infrastructure.Data.Repositories;

namespace CorePesaje.Application.Services;

public sealed class ControlPesoService(SqlProcedureRepository repository)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    public async Task<Dictionary<string, object?>> SaveAsync(ControlPesoDTO dto, CancellationToken ct)
    {
        return await repository.ExecuteInTransactionAsync(async session =>
        {
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            SqlProcedureResult result;
            try { result = await session.ControlPesoAsync(0, json, checked((int)dto.controlpesoId), cancellationToken: ct); }
            catch
            {
                session.MarkRollbackOnly();
                return new Dictionary<string, object?>();
            }
            var row = SqlProcedureRepository.StatusRow(result) ?? new Dictionary<string, object?>();
            var response = new Dictionary<string, object?>(row, StringComparer.Ordinal);
            if (SqlProcedureRepository.IsFailedStatus(row))
            {
                session.MarkRollbackOnly();
                return response;
            }
            var hasNewDetails = dto.detalles?.Any(d => d.detalleId == 0) == true;
            var index = ToLong(SqlProcedureRepository.Value(response, "Indice_Id")) ?? dto.controlpesoId;
            if ((dto.controlpesoId == 0 || hasNewDetails) && index > 0)
            {
                var indicesResult = await session.ControlPesoAsync(3, null, checked((int)index), cancellationToken: ct);
                var jsonIndices = SqlProcedureRepository.ConcatenateFirstColumn(indicesResult);
                using var document = JsonDocument.Parse(jsonIndices);
                var first = document.RootElement.ValueKind == JsonValueKind.Array && document.RootElement.GetArrayLength() > 0
                    ? document.RootElement[0] : default;
                response["detalles"] = first.ValueKind == JsonValueKind.Object && first.TryGetProperty("detalles", out var details)
                    ? JsonSerializer.Deserialize<List<ItemDetalleIndiceDTO>>(details.GetRawText(), JsonOptions)
                    : new List<ItemDetalleIndiceDTO>();
            }
            return response;
        }, ct);
    }

    public async Task<ControlPesoDTO?> GetByIdAsync(long id, CancellationToken ct)
    {
        SqlProcedureResult result;
        try { result = await repository.ControlPesoAsync(1, null, checked((int)id), cancellationToken: ct); }
        catch { return null; }
        var rows = result.resultSets.FirstOrDefault()?.rows;
        if (rows is null || rows.Count == 0) return null;
        var header = HeaderFromRow(rows[0], includeTotals: false);
        header.detalles = rows.Select(DetailFromRow).ToList();
        return header;
    }

    public async Task<List<ControlPesoDTO>> GetByDateAsync(long empresaId, long sucursalId, long almacenId,
        string fecha, int estado, CancellationToken ct)
    {
        try
        {
            var date = DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var result = await repository.ControlPesoAsync(2, null, 0, date, estado,
                checked((int)empresaId), checked((int)sucursalId), checked((int)almacenId), ct);
            var rows = result.resultSets.FirstOrDefault()?.rows ?? [];
            return rows.Select(r => HeaderFromRow(r, includeTotals: true)).ToList();
        }
        catch { return []; }
    }

    public Task<List<Dictionary<string, object?>>> GetComboAsync(long empresaId, long sucursalId,
        long almacenId, string fecha, CancellationToken ct) => repository.GuiasComboAsync(
            checked((int)empresaId), checked((int)sucursalId), checked((int)almacenId),
            DateOnly.ParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture), ct);

    public async Task UpdateStateAsync(long id, int estado, long usuarioId, CancellationToken ct)
    {
        await repository.ExecuteInTransactionAsync(async session =>
        {
            var result = await session.ActualizarEstadoAsync(1, id, estado, usuarioId, ct);
            var row = SqlProcedureRepository.StatusRow(result);
            if (SqlProcedureRepository.IsFailedStatus(row))
            {
                session.MarkRollbackOnly();
                throw new InvalidOperationException(Convert.ToString(SqlProcedureRepository.Value(row!, "Msg"), CultureInfo.InvariantCulture)
                    ?? "Error al actualizar el estado de ControlPeso.");
            }
            return true;
        }, ct);
    }

    private static ControlPesoDTO HeaderFromRow(Dictionary<string, object?> r, bool includeTotals) => new()
    {
        controlpesoId = ToLong(V(r, "controlpesoId")) ?? 0,
        empresaId = ToLong(V(r, "empresaId")) ?? 0,
        sucursalId = ToLong(V(r, "sucursalId")) ?? 0,
        almacenId = ToLong(V(r, "almacenId")) ?? 0,
        documentoId = ToLong(V(r, "documentoId")) ?? 0,
        serieId = ToLong(V(r, "serieId")) ?? 0,
        clienteId = ToLong(V(r, "clienteId")) ?? 0,
        numero = Str(V(r, "numero")),
        fecha = ToDateTime(V(r, "fecha")),
        cliente = Str(V(r, "cliente")),
        ptoPartida = Str(V(r, "ptoPartida")),
        ptoLlegada = Str(V(r, "ptoLlegada")),
        estado = ToInt(V(r, "estado")) ?? 0,
        afectaMerma = ToInt(V(r, "afectaMerma")) ?? 0,
        guiaRecepcionId = ToLong(V(r, "guiaRecepcionId")) ?? 0,
        pesajeId = ToLong(V(r, "pesajeId")) ?? 0,
        cantEnvase = includeTotals ? ToDouble(V(r, "cantEnvase")) : null,
        unidades = includeTotals ? ToDouble(V(r, "unidades")) : null,
        pesoBruto = includeTotals ? ToDouble(V(r, "pesoBruto")) : null,
        pesoTara = includeTotals ? ToDouble(V(r, "pesoTara")) : null,
        brutoCliente = includeTotals ? ToDouble(V(r, "brutoCliente")) : null,
        taraCliente = includeTotals ? ToDouble(V(r, "taraCliente")) : null,
        pesoNeto = includeTotals ? ToDouble(V(r, "pesoNeto")) : null,
        promedio = includeTotals ? ToDouble(V(r, "promedio")) : null
    };

    private static ControlPesoDetalleDTO DetailFromRow(Dictionary<string, object?> r) => new()
    {
        detalleId = ToLong(V(r, "detalleId")) ?? 0,
        controlpesoId = ToLong(V(r, "controlpesoId")) ?? 0,
        item = ToInt(V(r, "item")) ?? 0,
        productoId = ToLong(V(r, "productoId")) ?? 0,
        producto = Str(V(r, "producto")),
        envaseId = ToLong(V(r, "envaseId")) ?? 0,
        envase = Str(V(r, "envase")),
        cantEnvase = ToDouble(V(r, "cantEnvase")) ?? 0,
        unidades = ToDouble(V(r, "unidades")) ?? 0,
        precio = ToDouble(V(r, "precio")) ?? 0,
        pesoBruto = ToDouble(V(r, "pesoBruto")) ?? 0,
        pesoTara = ToDouble(V(r, "pesoTara")) ?? 0,
        brutoCliente = ToDouble(V(r, "brutoCliente")) ?? 0,
        taraCliente = ToDouble(V(r, "taraCliente")) ?? 0,
        pesoNeto = ToDouble(V(r, "pesoNeto")) ?? 0,
        promedio = ToDouble(V(r, "promedio")) ?? 0,
        sexo = Str(V(r, "sexo")),
        tipoTara = ToInt(V(r, "tipoTara")) ?? 0,
        tipoBruto = ToInt(V(r, "tipoBruto")) ?? 0,
        despachoStock = ToInt(V(r, "despachoStock")) ?? 0,
        horaTara = ToDateTime(V(r, "horaTara")),
        horaBruto = ToDateTime(V(r, "horaBruto")),
        usuarioId = ToLong(V(r, "usuarioId")) ?? 0,
        dispositivo = Str(V(r, "dispositivo")) ?? ""
    };

    private static object? V(Dictionary<string, object?> r, string key) => SqlProcedureRepository.Value(r, key);
    private static string? Str(object? v) => v is null ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
    private static long? ToLong(object? v) => v is null ? null : Convert.ToInt64(v, CultureInfo.InvariantCulture);
    private static int? ToInt(object? v) => v is null ? null : Convert.ToInt32(v, CultureInfo.InvariantCulture);
    private static double? ToDouble(object? v) => v is null ? null : Convert.ToDouble(v, CultureInfo.InvariantCulture);
    private static DateTime? ToDateTime(object? v) => v is null ? null : Convert.ToDateTime(v, CultureInfo.InvariantCulture);
}

public sealed class ItemDetalleIndiceDTO
{
    public long detalleId { get; set; }
    public long controlpesoId { get; set; }
    public int item { get; set; }
}
