using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Especialidades;

public interface IEspecialidadRepository
{
    Task<Especialidad?> ObtenerPorIdAsync(Guid especialidadId, CancellationToken cancellationToken = default);
    Task<Especialidad?> ObtenerRastreadaAsync(Guid especialidadId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Especialidad>> ListarActivasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Especialidad>> ListarTodasAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(Especialidad especialidad, CancellationToken cancellationToken = default);
}
