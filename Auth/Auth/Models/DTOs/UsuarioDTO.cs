using Auth.Models.Entities;

namespace Auth.Models.DTOs;

// passwordHash is part of the current Java wire contract; remove only in a separately approved phase.
public sealed record UsuarioDTO(
    int Id,
    string? Username,
    string? PasswordHash,
    string? Password,
    string? Nombre,
    string? Rol,
    bool Activo,
    string? FechaCre,
    string? FechaMod,
    string? Modifica)
{
    public static UsuarioDTO From(Usuario u) => new(
        u.Usuario_Id, u.Bre_Usu, u.password_hash, null, u.Nom_Usu, u.rol,
        u.Activo, u.Fecha_Cre?.ToString(), u.Fecha_Mod?.ToString(), u.Cla_Mod);
}
