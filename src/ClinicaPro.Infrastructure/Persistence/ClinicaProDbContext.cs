using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

public sealed class ClinicaProDbContext(DbContextOptions<ClinicaProDbContext> options)
    : DbContext(options)
{
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaProDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
