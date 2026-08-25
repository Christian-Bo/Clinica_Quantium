using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class PacienteConfiguration : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> builder)
    {
        builder.ToTable("Pacientes");

        builder.HasKey(paciente => paciente.Id);

        builder.Property(paciente => paciente.Id)
            .HasColumnName("PacienteId");

        builder.Property(paciente => paciente.UsuarioId)
            .IsRequired();

        builder.HasIndex(paciente => paciente.UsuarioId)
            .IsUnique();

        builder.Property(paciente => paciente.Nombres)
            .HasMaxLength(Paciente.NombresMaxLength)
            .IsRequired();

        builder.Property(paciente => paciente.Apellidos)
            .HasMaxLength(Paciente.ApellidosMaxLength)
            .IsRequired();

        builder.Property(paciente => paciente.Documento)
            .HasMaxLength(Paciente.DocumentoMaxLength);

        builder.Property(paciente => paciente.Telefono)
            .HasMaxLength(Paciente.TelefonoMaxLength);

        builder.Property(paciente => paciente.Direccion)
            .HasMaxLength(Paciente.DireccionMaxLength);

        builder.Property(paciente => paciente.IsActive)
            .IsRequired();

        builder.Property(paciente => paciente.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(paciente => paciente.NombreCompleto);
    }
}
