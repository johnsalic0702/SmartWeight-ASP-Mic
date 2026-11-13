using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;

namespace CorePesaje.Infrastructure.Data.Repositories;

public sealed record SqlResultSet(List<Dictionary<string, object?>> rows);
public sealed record SqlProcedureResult(List<SqlResultSet> resultSets);

public sealed class SqlProcedureRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("CorePesaje")
        ?? throw new InvalidOperationException("ConnectionStrings:CorePesaje no está configurada.");

    public async Task<SqlProcedureResult> ExecuteAsync(
        string procedure, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await ExecuteOnConnectionAsync(connection, null, procedure, parameters, cancellationToken);
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<SqlProcedureSession, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var session = new SqlProcedureSession((procedure, parameters, ct) =>
            ExecuteOnConnectionAsync(connection, transaction, procedure, parameters, ct));
        try
        {
            var result = await operation(session);
            if (session.RollbackOnly)
            {
                try { await transaction.RollbackAsync(cancellationToken); }
                catch { /* A failed SQL command can already have ended the server transaction. */ }
            }
            else
                await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try { await transaction.RollbackAsync(cancellationToken); }
            catch { /* The stored procedure may already have rolled back the connection transaction. */ }
            throw;
        }
    }

    private static async Task<SqlProcedureResult> ExecuteOnConnectionAsync(SqlConnection connection,
        SqlTransaction? transaction, string procedure, IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(procedure, connection, transaction) { CommandType = CommandType.StoredProcedure };
        foreach (var (name, value) in parameters)
        {
            var parameter = command.Parameters.Add(name, GetSqlType(name, value));
            if (name == "@JSON") parameter.Size = -1;
            parameter.Value = value ?? DBNull.Value;
        }

        var sets = new List<SqlResultSet>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        do
        {
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                rows.Add(row);
            }
            if (reader.FieldCount > 0) sets.Add(new SqlResultSet(rows));
        } while (await reader.NextResultAsync(cancellationToken));
        return new SqlProcedureResult(sets);
    }

    public Task<SqlProcedureResult> ControlPesoAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int estado = -1, int empresaId = 1, int sucursalId = 1, int almacenId = 1,
        CancellationToken cancellationToken = default) => ExecuteAsync("dbo.Web_ControlPeso",
        ControlPesoParameters(tipo, json, id, fecha, estado, empresaId, sucursalId, almacenId), cancellationToken);

    public Task<SqlProcedureResult> GuiaAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int? estado = null, int? empresaId = 1, int? sucursalId = 1, int? almacenId = 1,
        CancellationToken cancellationToken = default) => ExecuteAsync("dbo.Web_Guia",
        GuiaParameters(tipo, json, id, fecha, estado, empresaId, sucursalId, almacenId), cancellationToken);

    public Task<SqlProcedureResult> PedidoAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int? estado = null, int empresaId = 1, CancellationToken cancellationToken = default) => ExecuteAsync("dbo.Web_PedidoVenta",
        PedidoParameters(tipo, json, id, fecha, estado, empresaId), cancellationToken);

    public Task<SqlProcedureResult> ActualizarEstadoAsync(int tipo, long id, int estado, long usuarioId,
        CancellationToken cancellationToken = default) => ExecuteAsync("dbo.Web_ActualizaEstado",
        StateParameters(tipo, id, estado, usuarioId), cancellationToken);

    public Task<SqlProcedureResult> TicketAsync(long id, CancellationToken cancellationToken = default) => ExecuteAsync("dbo.Rpt_ControlPesoQR",
        new Dictionary<string, object?> { ["@Tipo"] = 2, ["@Id"] = checked((int)id) }, cancellationToken);

    public async Task<List<Dictionary<string, object?>>> GuiasComboAsync(int empresaId, int sucursalId,
        int almacenId, DateOnly fecha, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT GuiaRemision_Id AS id, CONVERT(VARCHAR(3), G.Serie_Id) + '-' + G.sGReNumero AS nombre " +
                           "FROM GuiaRemision G WHERE G.nGReEstado <> 0 AND G.Movimiento_Id = 1 " +
                           "AND G.Empresa_Id = @empresa AND G.Sucursal_Id = @sucursal AND G.Almacen_Id = @almacen AND G.dGReFecha = @fecha";
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@empresa", SqlDbType.Int).Value = empresaId;
        command.Parameters.Add("@sucursal", SqlDbType.Int).Value = sucursalId;
        command.Parameters.Add("@almacen", SqlDbType.Int).Value = almacenId;
        command.Parameters.Add("@fecha", SqlDbType.Date).Value = fecha.ToDateTime(TimeOnly.MinValue);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new Dictionary<string, object?> { ["id"] = Convert.ToInt64(reader.GetValue(0)), ["nombre"] = reader.IsDBNull(1) ? null : reader.GetString(1) });
        return rows;
    }

    public static string ConcatenateFirstColumn(SqlProcedureResult result)
    {
        if (result.resultSets.Count == 0) return "[]";
        var text = new StringBuilder();
        foreach (var row in result.resultSets[0].rows)
            if (row.Count > 0 && row.First().Value is not null)
                text.Append(Convert.ToString(row.First().Value, CultureInfo.InvariantCulture));
        return string.IsNullOrWhiteSpace(text.ToString()) ? "[]" : text.ToString();
    }

    public static Dictionary<string, object?>? StatusRow(SqlProcedureResult result)
    {
        // INSERT branches can emit an initial ID result set. Prefer the final Indice_Id/Msg set.
        foreach (var set in result.resultSets.AsEnumerable().Reverse())
            if (set.rows.Any(row => row.ContainsKey("Indice_Id"))) return set.rows.FirstOrDefault();
        return result.resultSets.SelectMany(set => set.rows).FirstOrDefault();
    }

    public static bool IsFailedStatus(Dictionary<string, object?>? row) =>
        row is not null && row.TryGetValue("Indice_Id", out var value) && value is not null &&
        Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0;

    public static object? Value(Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) ? value : null;

    private static SqlDbType GetSqlType(string name, object? value) => name switch
    {
        "@JSON" => SqlDbType.NVarChar,
        "@DesFec" => SqlDbType.Date,
        _ when value is string => SqlDbType.NVarChar,
        _ when value is int => SqlDbType.Int,
        _ when value is long => SqlDbType.BigInt,
        _ when value is DateTime => SqlDbType.Date,
        _ => SqlDbType.Int
    };

    internal static IReadOnlyDictionary<string, object?> ControlPesoParameters(int tipo, string? json, int id,
        DateOnly? fecha = null, int estado = -1, int empresaId = 1, int sucursalId = 1, int almacenId = 1)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["@TipoConsulta"] = tipo, ["@JSON"] = json, ["@Id"] = id
        };
        if (tipo == 2)
        {
            parameters["@DesFec"] = fecha?.ToDateTime(TimeOnly.MinValue);
            parameters["@Estado"] = estado;
            parameters["@Empresa_Id"] = empresaId;
            parameters["@Sucursal_Id"] = sucursalId;
            parameters["@Almacen_Id"] = almacenId;
        }
        return parameters;
    }

    internal static IReadOnlyDictionary<string, object?> GuiaParameters(int tipo, string? json, int id,
        DateOnly? fecha = null, int? estado = null, int? empresaId = 1, int? sucursalId = 1, int? almacenId = 1) =>
        new Dictionary<string, object?>
        {
            ["@TipoConsulta"] = tipo, ["@JSON"] = json, ["@Id"] = id,
            ["@DesFec"] = fecha?.ToDateTime(TimeOnly.MinValue), ["@Estado"] = estado,
            ["@Empresa_Id"] = empresaId, ["@Sucursal_Id"] = sucursalId, ["@Almacen_Id"] = almacenId
        };

    internal static IReadOnlyDictionary<string, object?> PedidoParameters(int tipo, string? json, int id,
        DateOnly? fecha = null, int? estado = null, int empresaId = 1) =>
        new Dictionary<string, object?>
        {
            ["@TipoConsulta"] = tipo, ["@JSON"] = json, ["@Id"] = id,
            ["@DesFec"] = fecha?.ToDateTime(TimeOnly.MinValue), ["@Estado"] = estado,
            ["@Empresa_Id"] = empresaId
        };

    internal static IReadOnlyDictionary<string, object?> StateParameters(int tipo, long id, int estado, long usuarioId) =>
        new Dictionary<string, object?>
        {
            ["@Tipo"] = checked((int)tipo), ["@Id"] = checked((int)id),
            ["@Estado"] = estado, ["@UsuarioId"] = checked((int)usuarioId)
        };
}

public sealed class SqlProcedureSession(
    Func<string, IReadOnlyDictionary<string, object?>, CancellationToken, Task<SqlProcedureResult>> execute)
{
    public bool RollbackOnly { get; private set; }
    public void MarkRollbackOnly() => RollbackOnly = true;

    public Task<SqlProcedureResult> ControlPesoAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int estado = -1, int empresaId = 1, int sucursalId = 1, int almacenId = 1,
        CancellationToken cancellationToken = default) => execute("dbo.Web_ControlPeso",
        SqlProcedureRepository.ControlPesoParameters(tipo, json, id, fecha, estado, empresaId, sucursalId, almacenId), cancellationToken);

    public Task<SqlProcedureResult> GuiaAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int? estado = null, int? empresaId = 1, int? sucursalId = 1, int? almacenId = 1,
        CancellationToken cancellationToken = default) => execute("dbo.Web_Guia",
        SqlProcedureRepository.GuiaParameters(tipo, json, id, fecha, estado, empresaId, sucursalId, almacenId), cancellationToken);

    public Task<SqlProcedureResult> PedidoAsync(int tipo, string? json, int id, DateOnly? fecha = null,
        int? estado = null, int empresaId = 1, CancellationToken cancellationToken = default) => execute("dbo.Web_PedidoVenta",
        SqlProcedureRepository.PedidoParameters(tipo, json, id, fecha, estado, empresaId), cancellationToken);

    public Task<SqlProcedureResult> ActualizarEstadoAsync(int tipo, long id, int estado, long usuarioId,
        CancellationToken cancellationToken = default) => execute("dbo.Web_ActualizaEstado",
        SqlProcedureRepository.StateParameters(tipo, id, estado, usuarioId), cancellationToken);
}
