using ClinicaPro.Domain.Entities;
using ClinicaPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

public sealed class ClinicaProDbContext
    : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid,
        ApplicationUserClaim,
        ApplicationUserRole,
        ApplicationUserLogin,
        ApplicationRoleClaim,
        ApplicationUserToken>
{
    public ClinicaProDbContext(DbContextOptions<ClinicaProDbContext> options)
        : base(options)
    {
    }

    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<Paciente> Pacientes => Set<Paciente>();
    public DbSet<Medico> Medicos => Set<Medico>();
    public DbSet<MedicoEspecialidad> MedicoEspecialidades => Set<MedicoEspecialidad>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<Cita> Citas => Set<Cita>();
    public DbSet<HistorialCita> HistorialCitas => Set<HistorialCita>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<IntentoNotificacion> IntentosNotificacion => Set<IntentoNotificacion>();
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaProDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ApplicationUserRole>())
        {
            if (entry.State == EntityState.Added && entry.Entity.AssignedAtUtc == default)
            {
                entry.Entity.AssignedAtUtc = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
