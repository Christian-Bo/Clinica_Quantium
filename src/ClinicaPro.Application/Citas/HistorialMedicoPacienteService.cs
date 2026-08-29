using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class HistorialMedicoPacienteService(IMedicoRepository medicos, IPacienteRepository pacientes, ICitaRepository citas)
{
    private static readonly HashSet<string> EstadosCerrados =
    [
        CitaEstados.Atendida,
        CitaEstados.Cancelada,
        CitaEstados.Rechazada,
        CitaEstados.NoPresentada
    ];

    public async Task<HistorialMedicoPaciente?> ExecuteAsync(
        Guid usuarioId,
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de médico.");

        var paciente = await pacientes.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return null;
        }

        var relacionadas = (await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken))
            .Where(cita => cita.MedicoId == medico.Id)
            .OrderByDescending(cita => cita.FechaHoraInicio)
            .ToList();

        if (relacionadas.Count == 0)
        {
            throw new ForbiddenException("El médico no tiene citas con este paciente.");
        }

        var ahora = HoraClinica.Ahora();
        var proximas = relacionadas
            .Where(cita => cita.FechaHoraInicio >= ahora && !EstadosCerrados.Contains(cita.Estado))
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToList();
        var pasadas = relacionadas
            .Where(cita => !proximas.Contains(cita))
            .ToList();
        var ultimaAtencion = relacionadas.FirstOrDefault(cita => cita.Estado == CitaEstados.Atendida);

        return new HistorialMedicoPaciente(paciente, ultimaAtencion, proximas, pasadas);
    }
}

public sealed record HistorialMedicoPaciente(
    Paciente Paciente,
    Cita? UltimaAtencion,
    IReadOnlyList<Cita> CitasProximas,
    IReadOnlyList<Cita> CitasPasadas);
