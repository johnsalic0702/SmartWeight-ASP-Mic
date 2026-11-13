using Auth.Models.DTOs;
using Auth.Models.Entities;
using Auth.Repositories;
using Auth.Security;
namespace Auth.Services;
public sealed class AuthService(IUsuarioRepository users, ITokenService tokens) : IAuthService
{
    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByUsername(request.Username, ct);
        if (user is null) throw new InvalidOperationException("Usuario no encontrado");
        if (!user.Activo) throw new InvalidOperationException("El usuario está inactivo.");
        if (user.password_hash is not null)
        {
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.password_hash))
                throw new InvalidOperationException("Credenciales incorrectas");
        }
        else
        {
            if (!string.Equals(user.Psw_Usu, request.Password, StringComparison.Ordinal))
                throw new InvalidOperationException("Credenciales incorrectas");
            user.password_hash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 10);
            await users.Save(user, ct);
        }
        var dto = UsuarioDTO.From(user);
        return new LoginResponse(tokens.Create(user), dto);
    }

    public async Task ChangePassword(string username, string newPassword, CancellationToken ct)
    {
        var user = await users.FindByUsername(username, ct)
            ?? throw new InvalidOperationException("Usuario no encontrado");
        user.password_hash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 10);
        await users.Save(user, ct);
    }

    public async Task<IReadOnlyList<UsuarioDTO>> GetUsers(long? lastSync, CancellationToken ct)
    {
        if (lastSync.HasValue)
            throw new IncrementalSyncUnavailableException();
        var all = await users.GetAll(ct);
        return all.Select(UsuarioDTO.From).ToArray();
    }

}

public sealed class IncrementalSyncUnavailableException : Exception { }
