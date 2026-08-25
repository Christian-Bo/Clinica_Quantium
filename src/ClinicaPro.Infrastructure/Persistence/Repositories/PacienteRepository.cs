using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class PacienteRepository(ClinicaProDbContext dbContext) : IPacienteRepository
{
    public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        return dbContext.Pacientes.AsNoTracking()
            .SingleOrDefaultAsync(paciente => paciente.Id == pacienteId && paciente.IsActive, cancellationToken);
    }

    public async Task<Paciente?> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Pacientes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                paciente => paciente.UsuarioId == usuarioId && paciente.IsActive,
                cancellationToken);
    }

    public async Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default)
    {
        await dbContext.Pacientes.AddAsync(paciente, cancellationToken);
    }
}
