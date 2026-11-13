using Maestros.Models;

namespace Maestros.DTOs;

public sealed class ListMaestroDTO
{
    public long Id { get; init; }
    public string? Descripcion { get; init; }
    public string? Valor1 { get; init; }
    public string? Valor2 { get; init; }
    public string? Valor3 { get; init; }
    public int Estado { get; init; }

    public static ListMaestroDTO From(ListMaestro maestro) => new()
    {
        Id = maestro.Id,
        Descripcion = maestro.Descripcion,
        Valor1 = maestro.Valor1,
        Valor2 = maestro.Valor2,
        Valor3 = maestro.Valor3,
        Estado = maestro.Estado
    };
}
