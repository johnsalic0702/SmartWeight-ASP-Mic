using Auth.Models.Entities;
namespace Auth.Repositories;
public interface IUsuarioRepository
{
    Task<Usuario?> FindByUsername(string username, CancellationToken ct);
    Task<IReadOnlyList<Usuario>> GetAll(CancellationToken ct);
    Task Save(Usuario usuario, CancellationToken ct);
}
