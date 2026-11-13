using Auth.Models.Entities;
namespace Auth.Security;
public interface ITokenService { string Create(Usuario usuario); }
