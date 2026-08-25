using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class EspecialidadConfiguration : IEntityTypeConfiguration<Especialidad>
{
    public void Configure(EntityTypeBuilder<Especialidad> builder)
    {
        builder.ToTable("Especialidades");

        builder.HasKey(especialidad => especialidad.Id);

        builder.Property(especialidad => especialidad.Id)
            .HasColumnName("EspecialidadId");

        builder.Property(especialidad => especialidad.Nombre)
            .HasMaxLength(Especialidad.NombreMaxLength)
            .IsRequired();

        builder.Property(especialidad => especialidad.Descripcion)
            .HasMaxLength(Especialidad.DescripcionMaxLength);

        builder.Property(especialidad => especialidad.IsActive)
            .IsRequired();

        builder.Property(especialidad => especialidad.CreatedAtUtc)
            .IsRequired();
    }
}
