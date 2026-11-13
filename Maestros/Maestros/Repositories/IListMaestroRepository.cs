using Maestros.Models;

namespace Maestros.Repositories;

public interface IListMaestroRepository
{
    Task<IReadOnlyList<ListMaestro>> ObtenerPorTipoConsultaAsync(
        int consultaId,
        long id,
        int tipo,
        CancellationToken cancellationToken = default);
}
