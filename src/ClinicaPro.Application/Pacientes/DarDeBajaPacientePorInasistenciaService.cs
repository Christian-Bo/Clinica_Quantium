using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public sealed class DarDeBajaPacientePorInasistenciaService(
    IPacienteRepository pacientes,
    ICitaRepository citas,
    IEliminarPacienteYUsuario eliminar,
    IAuditoriaWriter auditoria)
{
    public async Task ExecuteAsync(
        Guid pacienteId,
        Guid actorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorIdAsync(pacienteId, cancellationToken)
            ?? throw new DomainException("El paciente no existe o ya fue dado de baja.");

        var historial = await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken);
        PacienteBajaPorInasistencia.Exigir(historial);

        var correo = await pacientes.ObtenerEmailPorPacienteIdAsync(paciente.Id, cancellationToken);
        await eliminar.ExecuteAsync(paciente.Id, paciente.UsuarioId, cancellationToken);
        await auditoria.RegistrarAsync(
            actorUsuarioId,
            "DarDeBaja",
            "Paciente",
            paciente.Id.ToString(),
            correo,
            cancellationToken);
    }
}
