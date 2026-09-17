using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class AccesoExpedienteService(IMedicoRepository medicos, ICitaRepository citas)
{
    public async Task ExigirLecturaAsync(
        Guid usuarioId,
        bool esStaffMostrador,
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        if (esStaffMostrador)
        {
            return;
        }

        var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de médico.");

        var relacionadas = await citas.ListarPorPacienteAsync(pacienteId, cancellationToken);
        if (relacionadas.All(cita => cita.MedicoId != medico.Id))
        {
            throw new ForbiddenException("El médico no tiene citas con este paciente.");
        }
    }
}
