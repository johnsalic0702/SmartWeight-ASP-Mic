using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data;
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<Usuario>();
        e.ToTable("Usuarios");
        e.HasKey(x => x.Usuario_Id);
        e.Property(x => x.Usuario_Id).HasColumnName("Usuario_Id").ValueGeneratedNever();
        e.Property(x => x.Bre_Usu).HasColumnName("Bre_Usu").IsRequired();
        e.Property(x => x.password_hash).HasColumnName("password_hash");
        e.Property(x => x.Psw_Usu).HasColumnName("Psw_Usu");
        e.Property(x => x.Nom_Usu).HasColumnName("Nom_Usu");
        e.Property(x => x.rol).HasColumnName("rol");
        e.Property(x => x.Est_Usu).HasColumnName("Est_Usu");
        e.Property(x => x.Fecha_Cre).HasColumnName("Fecha_Cre");
        e.Property(x => x.Fecha_Mod).HasColumnName("Fecha_Mod");
        e.Property(x => x.Cla_Mod).HasColumnName("Cla_Mod");
        e.Ignore(x => x.Activo);
        e.HasIndex(x => x.Bre_Usu).IsUnique();
        e.Property(x => x.Psw_Usu).IsRequired();
    }
}
