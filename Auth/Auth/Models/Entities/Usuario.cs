namespace Auth.Models.Entities;
public sealed class Usuario
{
    public int Usuario_Id { get; set; }
    public string? Bre_Usu { get; set; }
    public string? password_hash { get; set; }
    public string? Psw_Usu { get; set; }
    public string? Nom_Usu { get; set; }
    public string? rol { get; set; }
    public byte Est_Usu { get; set; }
    public DateTime? Fecha_Cre { get; set; }
    public DateTime? Fecha_Mod { get; set; }
    public string? Cla_Mod { get; set; }
    public bool Activo => Est_Usu == 1;
}
