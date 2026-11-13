namespace Maestros.Models;

public sealed class ListMaestro
{
    public long Id { get; set; }
    public string? Descripcion { get; set; }
    public string? Valor1 { get; set; }
    public string? Valor2 { get; set; }
    public string? Valor3 { get; set; }
    public int Estado { get; set; }
}
