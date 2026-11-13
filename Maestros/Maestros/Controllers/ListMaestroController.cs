using Maestros.DTOs;
using Maestros.Services;
using Microsoft.AspNetCore.Mvc;

namespace Maestros.Controllers;

[ApiController]
[Route("maestros")]
public sealed class ListMaestroController(IListMaestroService service) : ControllerBase
{
    [HttpGet("consulta/{consultaId}/id/{id}/tipo/{tipo}")]
    public async Task<ActionResult<IReadOnlyList<ListMaestroDTO>>> BuscarPorTipoConsulta(
        int consultaId,
        long id,
        int tipo,
        CancellationToken cancellationToken)
    {
        try
        {
            var lista = await service.ObtenerPorTipoConsultaAsync(consultaId, id, tipo, cancellationToken);
            if (lista.Count == 0)
                return NoContent();

            return Ok(lista);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Ocurrió un error al consultar los maestros.");
        }
    }
}
