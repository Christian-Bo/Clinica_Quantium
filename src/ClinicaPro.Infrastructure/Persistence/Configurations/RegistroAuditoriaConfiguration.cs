using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class RegistroAuditoriaConfiguration : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> builder)
    {
        builder.ToTable("Auditoria");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("AuditoriaId");
        builder.Property(item => item.Accion).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Entidad).HasMaxLength(100).IsRequired();
        builder.Property(item => item.EntidadId).HasMaxLength(100);
        builder.Property(item => item.DireccionIp).HasMaxLength(45);
        builder.Property(item => item.CorrelationId).HasMaxLength(100);
        builder.Property(item => item.FechaUtc).HasColumnType("datetime2(0)");
    }
}
