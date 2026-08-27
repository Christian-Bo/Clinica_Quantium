using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Pacientes;

public interface IPacienteRepository
{
    Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default);
    Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Paciente?> ObtenerRastreadoPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);
    Task<string?> ObtenerEmailPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Paciente> Items, int Total)> BuscarAsync(
        string? texto,
        int omitir,
        int tomar,
        CancellationToken cancellationToken = default);
    Task<bool> ExisteDocumentoAsync(string documento, Guid? exceptoPacienteId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default);
}
