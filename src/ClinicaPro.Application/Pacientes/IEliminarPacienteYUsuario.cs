namespace ClinicaPro.Application.Pacientes;

public interface IEliminarPacienteYUsuario
{
    Task ExecuteAsync(Guid pacienteId, Guid usuarioId, CancellationToken cancellationToken = default);
}
