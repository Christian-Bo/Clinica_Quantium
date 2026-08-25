using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Pacientes;

public interface IPacienteRepository
{
    Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default);
    Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default);
}
