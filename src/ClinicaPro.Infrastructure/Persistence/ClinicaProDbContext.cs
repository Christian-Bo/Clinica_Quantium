using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

/// <summary>
/// Contexto EF Core de Clínica Pro.
/// Se mantiene sin DbSet hasta que las entidades del dominio sean implementadas y mapeadas.
/// </summary>
public sealed class ClinicaProDbContext(DbContextOptions<ClinicaProDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaProDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
