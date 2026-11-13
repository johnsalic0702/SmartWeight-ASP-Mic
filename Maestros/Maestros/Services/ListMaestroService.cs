using Maestros.DTOs;
using Maestros.Repositories;

namespace Maestros.Services;

public sealed class ListMaestroService(IListMaestroRepository repository) : IListMaestroService
{
    public async Task<IReadOnlyList<ListMaestroDTO>> ObtenerPorTipoConsultaAsync(
        int consultaId,
        long id,
        int tipo,
        CancellationToken cancellationToken = default)
    {
        var maestros = await repository.ObtenerPorTipoConsultaAsync(consultaId, id, tipo, cancellationToken);
        var lista = maestros.Select(ListMaestroDTO.From).ToList();

        if (lista.Count == 0)
            throw new InvalidOperationException("Service:No existen registros para esta consulta");

        return lista;
    }
}
