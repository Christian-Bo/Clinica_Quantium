using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Especialidades;

public interface IEspecialidadRepository
{
    Task<IReadOnlyList<Especialidad>> ListarActivasAsync(CancellationToken cancellationToken = default);
}
