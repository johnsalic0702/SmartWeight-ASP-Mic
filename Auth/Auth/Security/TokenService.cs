using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Configuration;
using Auth.Models.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace Auth.Security;
public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public string Create(Usuario usuario)
    {
        var now = DateTime.UtcNow;
        var claims = new[] { new Claim("sub", usuario.Bre_Usu ?? ""), new Claim("rol", usuario.rol ?? ""), new Claim("iat", new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Secret));
        var token = new JwtSecurityToken(claims: claims, expires: now.AddMilliseconds(options.Value.Expiration), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
