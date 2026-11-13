using Auth.Data;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace Auth.Repositories;
public sealed class UsuarioRepository(AuthDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> FindByUsername(string username, CancellationToken ct) => db.Usuarios.FirstOrDefaultAsync(x => x.Bre_Usu == username, ct);
    public async Task<IReadOnlyList<Usuario>> GetAll(CancellationToken ct) => await db.Usuarios.AsNoTracking().ToListAsync(ct);
    public async Task Save(Usuario usuario, CancellationToken ct) { await db.SaveChangesAsync(ct); }
}
