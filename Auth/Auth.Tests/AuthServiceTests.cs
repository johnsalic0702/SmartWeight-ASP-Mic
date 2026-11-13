using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Configuration;
using Auth.Controllers;
using Auth.Models.DTOs;
using Auth.Models.Entities;
using Auth.Repositories;
using Auth.Security;
using Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Auth.Tests;

public sealed class AuthServiceTests
{
    private const string Secret = "local-test-secret-with-adequate-length-for-hs256";
    private static (AuthService Service, MemoryUsers Users) Build(params Usuario[] records)
    {
        var users = new MemoryUsers(records);
        var tokens = new TokenService(Options.Create(new JwtOptions { Secret = Secret, Expiration = 43_200_000 }));
        return (new AuthService(users, tokens), users);
    }
    private static Usuario User(byte status = 1, string? role = "ADMIN", string? hash = null, string? plain = "pw") =>
        new() { Usuario_Id = 7, Bre_Usu = "alice", password_hash = hash, Psw_Usu = plain, Nom_Usu = "Alice", rol = role, Est_Usu = status, Fecha_Cre = new DateTime(2026, 1, 1), Fecha_Mod = new DateTime(2026, 1, 2), Cla_Mod = "sys" };

    [Fact]
    public async Task LoginCorrectoDevuelveTokenYUsuarioDto()
    {
        var (service, _) = Build(User());
        var response = await service.Login(new LoginRequest("alice", "pw"), default);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("alice", response.Usuario.Username);
        Assert.Equal(7, response.Usuario.Id);
        Assert.True(response.Usuario.Activo);
        Assert.Null(response.Usuario.Password);
        Assert.NotNull(response.Usuario.PasswordHash);
    }

    [Fact]
    public async Task UsuarioInexistenteMantieneErrorDeJava() =>
        Assert.Equal("Usuario no encontrado", (await Assert.ThrowsAsync<InvalidOperationException>(() => Build().Service.Login(new LoginRequest("missing", "pw"), default))).Message);

    [Fact]
    public async Task PasswordIncorrectoEsRechazado()
    {
        var (service, _) = Build(User());
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Login(new LoginRequest("alice", "bad"), default));
        Assert.Equal("Credenciales incorrectas", ex.Message);
    }

    [Theory]
    [InlineData((byte)0)] [InlineData((byte)2)]
    public async Task UsuarioInactivoEsRechazado(byte estado)
    {
        var (service, _) = Build(User(status: estado));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Login(new LoginRequest("alice", "pw"), default));
        Assert.Equal("El usuario está inactivo.", ex.Message);
    }

    [Fact]
    public async Task BCryptValidaHashGeneradoPorJava()
    {
        const string javaHash = "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
        var (service, _) = Build(User(hash: javaHash, plain: null));
        await service.Login(new LoginRequest("alice", "password"), default);
    }

    [Fact]
    public async Task MigraPasswordHeredadoConCostoDeSpringPorDefecto()
    {
        var (service, users) = Build(User());
        await service.Login(new LoginRequest("alice", "pw"), default);
        Assert.True(BCrypt.Net.BCrypt.Verify("pw", users.Records[0].password_hash));
        Assert.Equal("10", users.Records[0].password_hash!.Substring(4, 2));
    }

    [Fact]
    public async Task CambioDePasswordSoloActualizaPasswordHash()
    {
        var originalLegacyPassword = "legacy";
        var (service, users) = Build(User(plain: originalLegacyPassword));
        await service.ChangePassword("alice", "new-password", default);
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", users.Records[0].password_hash));
        Assert.Equal(originalLegacyPassword, users.Records[0].Psw_Usu);
    }

    [Fact]
    public async Task JwtUsaSubRolIatExpYFirmaHs256Validable()
    {
        var (service, _) = Build(User());
        var login = await service.Login(new LoginRequest("alice", "pw"), default);
        var handler = new JwtSecurityTokenHandler();
        handler.MapInboundClaims = false;
        var principal = handler.ValidateToken(login.Token, new TokenValidationParameters
        {
            ValidateIssuer = false, ValidateAudience = false, ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, NameClaimType = "sub", RoleClaimType = "rol"
        }, out var validated);
        var jwt = (JwtSecurityToken)validated;
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        Assert.Equal("alice", jwt.Payload["sub"]?.ToString());
        Assert.Equal("ADMIN", jwt.Payload["rol"]?.ToString());
        Assert.Equal("alice", principal.FindFirst("sub")?.Value);
        Assert.Equal("ADMIN", principal.FindFirst("rol")?.Value);
        var issuedAt = Convert.ToInt64(jwt.Payload["iat"]);
        var expiresAt = Convert.ToInt64(jwt.Payload["exp"]);
        Assert.Equal(43_200, expiresAt - issuedAt);
        Assert.InRange(jwt.ValidTo - DateTime.UtcNow, TimeSpan.FromHours(11.9), TimeSpan.FromHours(12));
    }

    [Fact]
    public async Task AdminPuedeAccederAlEndpointAdmin()
    {
        Assert.True(await HasRole("ADMIN", "ADMIN"));
        Assert.Equal("ADMIN", Roles(nameof(AuthController.AdminTest)));
    }

    [Fact]
    public async Task OperadorPuedeAccederAlEndpointOperador()
    {
        Assert.True(await HasRole("OPERADOR", "ADMIN", "OPERADOR"));
        Assert.Equal(new[] { "ADMIN", "OPERADOR" }, Roles(nameof(AuthController.OperadorTest)).Split(','));
    }

    [Fact]
    public async Task AdminTambienPuedeAccederAlEndpointOperador() => Assert.True(await HasRole("ADMIN", "ADMIN", "OPERADOR"));

    [Fact]
    public async Task OperadorNoPuedeAccederAlEndpointAdmin() => Assert.False(await HasRole("OPERADOR", "ADMIN"));

    [Fact]
    public void EndpointPublicoConservaRutaYRespuestaJava()
    {
        var controller = new AuthController(Build(User()).Service);
        Assert.Equal("Acceso permitido", controller.Test());
        Assert.NotNull(typeof(AuthController).GetMethod(nameof(AuthController.Test))!.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
        Assert.Equal("api/auth", typeof(AuthController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template);
    }

    [Fact]
    public async Task CambioPasswordConservaRespuestaJava()
    {
        var controller = new AuthController(Build(User()).Service);
        Assert.Equal("Contraseña actualizada correctamente", await controller.Password("alice", "new-password", default));
    }

    [Fact]
    public async Task LastSyncNoInventaConsultaYExponeLimitacionDocumentada()
    {
        var (service, _) = Build(User());
        await Assert.ThrowsAsync<IncrementalSyncUnavailableException>(() => service.GetUsers(10L, default));
    }

    private static string Roles(string methodName) => typeof(AuthController).GetMethod(methodName)!
        .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Roles!;

    private static async Task<bool> HasRole(string role, params string[] allowed)
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var identity = new ClaimsIdentity(new[] { new Claim("rol", role) }, "test", ClaimTypes.Name, "rol");
        var policy = new AuthorizationPolicyBuilder().RequireRole(allowed).Build();
        return (await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(new ClaimsPrincipal(identity), null, policy)).Succeeded;
    }

    private sealed class MemoryUsers(params Usuario[] records) : IUsuarioRepository
    {
        public Usuario[] Records { get; } = records;
        public Task<Usuario?> FindByUsername(string username, CancellationToken ct) => Task.FromResult(Records.FirstOrDefault(x => x.Bre_Usu == username));
        public Task<IReadOnlyList<Usuario>> GetAll(CancellationToken ct) => Task.FromResult<IReadOnlyList<Usuario>>(Records);
        public Task Save(Usuario usuario, CancellationToken ct) => Task.CompletedTask;
    }
}
