using Maestros.DTOs;

namespace Maestros.Services;

public interface IListMaestroService
{
    Task<IReadOnlyList<ListMaestroDTO>> ObtenerPorTipoConsultaAsync(
        int consultaId,
        long id,
        int tipo,
        CancellationToken cancellationToken = default);
}
