using ClinicaPro.Application.Especialidades;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class EspecialidadRepository(ClinicaProDbContext dbContext) : IEspecialidadRepository
{
    public Task<Especialidad?> ObtenerPorIdAsync(
        Guid especialidadId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Especialidades.AsNoTracking()
            .FirstOrDefaultAsync(
                especialidad => especialidad.Id == especialidadId && especialidad.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Especialidad>> ListarActivasAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Especialidades
            .AsNoTracking()
            .Where(especialidad => especialidad.IsActive)
            .OrderBy(especialidad => especialidad.Nombre)
            .ToListAsync(cancellationToken);
    }

    public Task<Especialidad?> ObtenerRastreadaAsync(
        Guid especialidadId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Especialidades.FirstOrDefaultAsync(
            especialidad => especialidad.Id == especialidadId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Especialidad>> ListarTodasAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Especialidades
            .AsNoTracking()
            .OrderBy(especialidad => especialidad.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Especialidad especialidad, CancellationToken cancellationToken = default)
    {
        await dbContext.Especialidades.AddAsync(especialidad, cancellationToken);
    }
}
