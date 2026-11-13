using System.Data;
using System.Globalization;
using Maestros.Models;
using Microsoft.Data.SqlClient;

namespace Maestros.Repositories;

public sealed class ListMaestroRepository(IConfiguration configuration, ILogger<ListMaestroRepository> logger)
    : IListMaestroRepository
{
    private const string ProcedureName = "dbo.Cmm_GetMaestros";
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection before starting Maestros.");

    public async Task<IReadOnlyList<ListMaestro>> ObtenerPorTipoConsultaAsync(
        int consultaId,
        long id,
        int tipo,
        CancellationToken cancellationToken = default)
    {
        var maestros = new List<ListMaestro>();

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(ProcedureName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add("@Consulta", SqlDbType.Int).Value = consultaId;
            // Java usa PreparedStatement.setLong para el segundo parámetro.
            command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;
            command.Parameters.Add("@Tipo", SqlDbType.Int).Value = tipo;

            await connection.OpenAsync(cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                maestros.Add(Map(reader, consultaId));
        }
        catch (Exception exception)
        {
            // Java registra el error y devuelve una lista vacía; el servicio termina convirtiéndolo en error.
            logger.LogError(exception, "Error al obtener Datos");
            return [];
        }

        return maestros;
    }

    private static ListMaestro Map(SqlDataReader reader, int consultaId)
    {
        var maestro = new ListMaestro();

        switch (consultaId)
        {
            case 1:
                maestro.Id = ReadLong(reader, "Empresa_Id");
                maestro.Valor1 = ReadString(reader, "Valor1");
                maestro.Valor2 = ReadString(reader, "Valor2");
                maestro.Valor3 = ReadString(reader, "Valor3");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 2:
                maestro.Id = ReadLong(reader, "Parametro_Id");
                maestro.Valor1 = ReadString(reader, "Valor1");
                maestro.Valor2 = ReadString(reader, "Valor2");
                maestro.Valor3 = ReadString(reader, "Valor3");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 3:
                maestro.Id = ReadLong(reader, "Almacen_Id");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 5:
                maestro.Id = ReadLong(reader, "Persona_Id");
                maestro.Valor1 = ReadString(reader, "Valor1");
                maestro.Valor2 = ReadString(reader, "Valor2");
                maestro.Valor3 = ReadString(reader, "Valor3");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 8:
                maestro.Id = ReadLong(reader, "Producto_Id");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 13:
                maestro.Id = ReadLong(reader, "Conductor_Id");
                maestro.Valor1 = ReadString(reader, "Valor1");
                maestro.Valor2 = ReadString(reader, "Valor2");
                maestro.Valor3 = ReadString(reader, "Valor3");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 14:
                maestro.Id = ReadLong(reader, "Vehiculo_Id");
                maestro.Valor1 = ReadString(reader, "Valor1");
                maestro.Valor2 = ReadString(reader, "Valor2");
                maestro.Valor3 = ReadString(reader, "Valor3");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 21:
                maestro.Id = ReadLong(reader, "Envase_Id");
                maestro.Valor1 = ReadString(reader, "valor1");
                maestro.Estado = ReadInt(reader, "Estado");
                break;
            case 53:
                maestro.Id = ReadLong(reader, "Id");
                maestro.Descripcion = ReadString(reader, "Descripcion");
                return maestro;
            default:
                throw new InvalidOperationException($"consultaId {consultaId} no está dentro del alcance de SmartWeight.");
        }

        // Java lee Nombre después de procesar las consultas distintas de 50–55.
        maestro.Descripcion = ReadString(reader, "Nombre");
        return maestro;
    }

    private static long ReadLong(SqlDataReader reader, string column) =>
        Convert.ToInt64(reader[column], CultureInfo.InvariantCulture);

    private static int ReadInt(SqlDataReader reader, string column) =>
        Convert.ToInt32(reader[column], CultureInfo.InvariantCulture);

    // JDBC ResultSet.getString también convierte valores SQL no textuales a texto.
    // Se deja el formato predeterminado del valor, sin imponer un patrón de fecha.
    private static string? ReadString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.CurrentCulture);
    }
}
