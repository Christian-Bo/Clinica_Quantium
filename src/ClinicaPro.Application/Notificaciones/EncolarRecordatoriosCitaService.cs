using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ClinicaPro.Application.Notificaciones;

public sealed class EncolarRecordatoriosCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    IEspecialidadRepository especialidades,
    INotificacionRepository notificaciones,
    IUnitOfWork unitOfWork,
    ILogger<EncolarRecordatoriosCitaService> logger)
{
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var ahora = HoraClinica.Ahora();
        var pendientes = await citas.ListarParaRecordatorioAsync(
            ahora.AddHours(20),
            ahora.AddHours(26),
            cancellationToken);

        var encolados = 0;
        foreach (var cita in pendientes)
        {
            if (await notificaciones.ExistePorCitaYTipoAsync(
                    cita.Id,
                    NotificacionTipos.RecordatorioCita,
                    cancellationToken))
            {
                continue;
            }

            try
            {
                var email = await pacientes.ObtenerEmailPorPacienteIdAsync(cita.PacienteId, cancellationToken);
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var paciente = await pacientes.ObtenerPorIdAsync(cita.PacienteId, cancellationToken);
                var medico = await medicos.ObtenerPorIdAsync(cita.MedicoId, cancellationToken);
                var especialidad = await especialidades.ObtenerPorIdAsync(cita.EspecialidadId, cancellationToken);
                var cuando = $"{cita.FechaHoraInicio:yyyy-MM-dd HH:mm} a {cita.FechaHoraFin:HH:mm} (hora de Guatemala)";

                await notificaciones.AgregarAsync(
                    Notificacion.EncolarEmail(
                        cita.PacienteId,
                        cita.Id,
                        NotificacionTipos.RecordatorioCita,
                        email,
                        "Clínica Pro — recordatorio de su cita",
                        $"Hola {paciente?.NombreCompleto ?? "paciente"},\n\nLe recordamos su cita de {especialidad?.Nombre ?? "su especialidad"} con {medico?.NombreCompleto ?? "el médico asignado"}.\nFecha y hora: {cuando}.\n\nSi no puede asistir, cancele con al menos 2 horas de anticipación."),
                    cancellationToken);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                encolados++;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "No se pudo encolar el recordatorio de la cita {CitaId}.",
                    cita.Id);
            }
        }

        return encolados;
    }
}
