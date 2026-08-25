using ClinicaPro.Application.Especialidades;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class EspecialidadRepository(ClinicaProDbContext dbContext) : IEspecialidadRepository
{
    public async Task<IReadOnlyList<Especialidad>> ListarActivasAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Especialidades
            .AsNoTracking()
            .Where(especialidad => especialidad.IsActive)
            .OrderBy(especialidad => especialidad.Nombre)
            .ToListAsync(cancellationToken);
    }
}
