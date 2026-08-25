using ClinicaPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Usuarios");

        builder.Property(usuario => usuario.Id)
            .HasColumnName("UsuarioId");

        builder.Property(usuario => usuario.PasswordHash)
            .HasMaxLength(500);

        builder.Property(usuario => usuario.SecurityStamp)
            .HasMaxLength(100);

        builder.Property(usuario => usuario.ConcurrencyStamp)
            .HasMaxLength(100);

        builder.Property(usuario => usuario.PhoneNumber)
            .HasMaxLength(30);
    }
}

public sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles");

        builder.Property(rol => rol.Id)
            .HasColumnName("RoleId");

        builder.Property(rol => rol.Name)
            .HasMaxLength(100);

        builder.Property(rol => rol.NormalizedName)
            .HasMaxLength(100);

        builder.Property(rol => rol.ConcurrencyStamp)
            .HasMaxLength(100);
    }
}

public sealed class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
{
    public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
    {
        builder.ToTable("UsuarioRoles");

        builder.Property(usuarioRol => usuarioRol.UserId)
            .HasColumnName("UsuarioId");

        builder.Property(usuarioRol => usuarioRol.RoleId)
            .HasColumnName("RoleId");

        builder.Property(usuarioRol => usuarioRol.AssignedAtUtc)
            .IsRequired();
    }
}

public sealed class ApplicationUserClaimConfiguration : IEntityTypeConfiguration<ApplicationUserClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationUserClaim> builder)
    {
        builder.ToTable("UsuarioClaims");

        builder.Property(claim => claim.UserId)
            .HasColumnName("UsuarioId");
    }
}

public sealed class ApplicationRoleClaimConfiguration : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("RolClaims");
    }
}

public sealed class ApplicationUserLoginConfiguration : IEntityTypeConfiguration<ApplicationUserLogin>
{
    public void Configure(EntityTypeBuilder<ApplicationUserLogin> builder)
    {
        builder.ToTable("UsuarioLogins");

        builder.Property(login => login.UserId)
            .HasColumnName("UsuarioId");
    }
}

public sealed class ApplicationUserTokenConfiguration : IEntityTypeConfiguration<ApplicationUserToken>
{
    public void Configure(EntityTypeBuilder<ApplicationUserToken> builder)
    {
        builder.ToTable("UsuarioTokens");

        builder.Property(token => token.UserId)
            .HasColumnName("UsuarioId");
    }
}
