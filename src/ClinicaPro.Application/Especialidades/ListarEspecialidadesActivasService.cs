using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Especialidades;

public sealed class ListarEspecialidadesActivasService(IEspecialidadRepository repository)
{
    public Task<IReadOnlyList<Especialidad>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return repository.ListarActivasAsync(cancellationToken);
    }
}
