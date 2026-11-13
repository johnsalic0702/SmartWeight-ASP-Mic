using Auth.Models.DTOs;
using Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Auth.Controllers;
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken ct) => auth.Login(request, ct);
  
    [HttpGet("usuarios")]
    [Authorize]
    public Task<IReadOnlyList<UsuarioDTO>> Usuarios([FromQuery] long? lastSync, CancellationToken ct) => auth.GetUsers(lastSync, ct);
    
    [HttpGet("test")]
    [AllowAnonymous]
    public string Test() => "Acceso permitido";
    
    [HttpGet("admin/test")]
    [Authorize(Roles = "ADMIN")]
    public string AdminTest() => "Acceso ADMIN correcto";
    
    [HttpGet("operador/test")]
    [Authorize(Roles = "ADMIN,OPERADOR")]
    public string OperadorTest() => "Acceso OPERADOR correcto";
    
    [HttpPut("password")]
    [Authorize]
    public async Task<string> Password([FromQuery(Name = "username")] string username, [FromQuery(Name = "nuevoPassword")] string nuevoPassword, CancellationToken ct)
    {
        await auth.ChangePassword(username, nuevoPassword, ct);
        return "Contraseña actualizada correctamente";
    }
}
