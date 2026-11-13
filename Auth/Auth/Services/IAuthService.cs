using Auth.Models.DTOs;
using Auth.Models.Entities;
namespace Auth.Services;
public interface IAuthService
{
    Task<LoginResponse> Login(LoginRequest request, CancellationToken ct);
    Task ChangePassword(string username, string newPassword, CancellationToken ct);
    Task<IReadOnlyList<UsuarioDTO>> GetUsers(long? lastSync, CancellationToken ct);
}
