using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class MedicoConfiguration : IEntityTypeConfiguration<Medico>
{
    public void Configure(EntityTypeBuilder<Medico> builder)
    {
        builder.ToTable("Medicos");
        builder.HasKey(medico => medico.Id);
        builder.Property(medico => medico.Id).HasColumnName("MedicoId");
        builder.HasMany<Horario>().WithOne().HasForeignKey(horario => horario.MedicoId);
        builder.HasMany<MedicoEspecialidad>().WithOne().HasForeignKey(relacion => relacion.MedicoId);
        builder.Property(medico => medico.Nombres).HasMaxLength(Medico.NombresMaxLength).IsRequired();
        builder.Property(medico => medico.Apellidos).HasMaxLength(Medico.ApellidosMaxLength).IsRequired();
        builder.Property(medico => medico.NumeroColegiado).HasMaxLength(Medico.ColegiadoMaxLength);
        builder.Property(medico => medico.Telefono).HasMaxLength(Medico.TelefonoMaxLength);
        builder.HasIndex(medico => medico.UsuarioId).IsUnique();
        builder.Ignore(medico => medico.NombreCompleto);
    }
}

public sealed class MedicoEspecialidadConfiguration : IEntityTypeConfiguration<MedicoEspecialidad>
{
    public void Configure(EntityTypeBuilder<MedicoEspecialidad> builder)
    {
        builder.ToTable("MedicoEspecialidad");
        builder.HasKey(relacion => new { relacion.MedicoId, relacion.EspecialidadId });
    }
}

public sealed class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        builder.ToTable("Horarios");
        builder.HasKey(horario => horario.Id);
        builder.Property(horario => horario.Id).HasColumnName("HorarioId");
        builder.Property(horario => horario.HoraInicio).HasColumnType("time(0)");
        builder.Property(horario => horario.HoraFin).HasColumnType("time(0)");
        builder.Property(horario => horario.VigenteDesde).HasColumnType("date");
        builder.Property(horario => horario.VigenteHasta).HasColumnType("date");
    }
}

public sealed class CitaConfiguration : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> builder)
    {
        builder.ToTable("Citas", table =>
        {
            table.HasTrigger("TR_Citas_Validaciones");
            table.HasTrigger("TR_Citas_Historial");
        });
        builder.HasKey(cita => cita.Id);
        builder.Property(cita => cita.Id).HasColumnName("CitaId");
        builder.Property(cita => cita.FechaHoraInicio).HasColumnType("datetime2(0)");
        builder.Property(cita => cita.FechaHoraFin).HasColumnType("datetime2(0)");
        builder.Property(cita => cita.MotivoConsulta).HasMaxLength(Cita.MotivoMaxLength).IsRequired();
        builder.Property(cita => cita.Estado).HasMaxLength(20).IsRequired();
        builder.Property(cita => cita.RowVersion).IsRowVersion();
    }
}

public sealed class HistorialCitaConfiguration : IEntityTypeConfiguration<HistorialCita>
{
    public void Configure(EntityTypeBuilder<HistorialCita> builder)
    {
        builder.ToTable("HistorialCitas");
        builder.HasKey(historial => historial.Id);
        builder.Property(historial => historial.Id).HasColumnName("HistorialCitaId");
        builder.Property(historial => historial.TipoCambio).HasMaxLength(30).IsRequired();
        builder.Property(historial => historial.EstadoAnterior).HasMaxLength(20);
        builder.Property(historial => historial.EstadoNuevo).HasMaxLength(20);
        builder.Property(historial => historial.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(historial => historial.FechaHoraInicioAnterior).HasColumnType("datetime2(0)");
        builder.Property(historial => historial.FechaHoraInicioNueva).HasColumnType("datetime2(0)");
        builder.Property(historial => historial.FechaHoraFinAnterior).HasColumnType("datetime2(0)");
        builder.Property(historial => historial.FechaHoraFinNueva).HasColumnType("datetime2(0)");
        builder.Property(historial => historial.FechaCambioUtc).HasColumnType("datetime2(0)");
    }
}

public sealed class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> builder)
    {
        builder.ToTable("Parametros");
        builder.HasKey(parametro => parametro.Clave);
        builder.Property(parametro => parametro.Clave).HasMaxLength(100);
        builder.Property(parametro => parametro.Valor).HasMaxLength(500).IsRequired();
        builder.Property(parametro => parametro.TipoDato).HasMaxLength(20).IsRequired();
        builder.Property(parametro => parametro.Descripcion).HasMaxLength(300);
    }
}
