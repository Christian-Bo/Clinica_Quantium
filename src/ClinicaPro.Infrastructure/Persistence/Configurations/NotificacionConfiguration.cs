using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.ToTable("Notificaciones");
        builder.HasKey(notificacion => notificacion.Id);
        builder.Property(notificacion => notificacion.Id).HasColumnName("NotificacionId");
        builder.Property(notificacion => notificacion.Canal).HasMaxLength(20).IsRequired();
        builder.Property(notificacion => notificacion.Tipo).HasMaxLength(50).IsRequired();
        builder.Property(notificacion => notificacion.Destinatario).HasMaxLength(256).IsRequired();
        builder.Property(notificacion => notificacion.Asunto).HasMaxLength(200);
        builder.Property(notificacion => notificacion.Contenido).IsRequired();
        builder.Property(notificacion => notificacion.Estado).HasMaxLength(20).IsRequired();
        builder.Property(notificacion => notificacion.UltimoError).HasMaxLength(1000);
        builder.Property(notificacion => notificacion.ProximoIntentoUtc).HasColumnType("datetime2(0)");
        builder.Property(notificacion => notificacion.EnviadaAtUtc).HasColumnType("datetime2(0)");
        builder.Property(notificacion => notificacion.CreatedAtUtc).HasColumnType("datetime2(0)");
    }
}

public sealed class IntentoNotificacionConfiguration : IEntityTypeConfiguration<IntentoNotificacion>
{
    public void Configure(EntityTypeBuilder<IntentoNotificacion> builder)
    {
        builder.ToTable("IntentosNotificacion");
        builder.HasKey(intento => intento.Id);
        builder.Property(intento => intento.Id).HasColumnName("IntentoNotificacionId");
        builder.Property(intento => intento.FechaIntentoUtc).HasColumnType("datetime2(0)");
        builder.Property(intento => intento.CodigoProveedor).HasMaxLength(100);
        builder.Property(intento => intento.RespuestaProveedor).HasMaxLength(1000);
    }
}
