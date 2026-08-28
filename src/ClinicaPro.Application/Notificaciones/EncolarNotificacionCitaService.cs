using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ClinicaPro.Application.Notificaciones;

public sealed class EncolarNotificacionCitaService(
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    IEspecialidadRepository especialidades,
    INotificacionRepository notificaciones,
    IUnitOfWork unitOfWork,
    ILogger<EncolarNotificacionCitaService> logger)
{
    public async Task ExecuteAsync(
        Cita cita,
        CancellationToken cancellationToken = default,
        string? tipo = null)
    {
        tipo ??= NotificacionTipos.DesdeEstadoCita(cita.Estado);
        if (tipo is null)
        {
            return;
        }

        try
        {
            var email = await pacientes.ObtenerEmailPorPacienteIdAsync(cita.PacienteId, cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning(
                    "No se encoló aviso {Tipo} para la cita {CitaId}: el paciente no tiene correo.",
                    tipo,
                    cita.Id);
                return;
            }

            var paciente = await pacientes.ObtenerPorIdAsync(cita.PacienteId, cancellationToken);
            var medico = await medicos.ObtenerPorIdAsync(cita.MedicoId, cancellationToken);
            var especialidad = await especialidades.ObtenerPorIdAsync(cita.EspecialidadId, cancellationToken);

            var (asunto, contenido) = Redactar(
                tipo,
                paciente?.NombreCompleto ?? "paciente",
                especialidad?.Nombre ?? "su especialidad",
                medico?.NombreCompleto ?? "el médico asignado",
                cita.FechaHoraInicio,
                cita.FechaHoraFin);

            await notificaciones.AgregarAsync(
                Notificacion.EncolarEmail(
                    cita.PacienteId,
                    cita.Id,
                    tipo,
                    email,
                    asunto,
                    contenido),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "No se pudo encolar el correo {Tipo} de la cita {CitaId}. La cita sí quedó guardada.",
                tipo,
                cita.Id);
        }
    }

    private static (string Asunto, string Contenido) Redactar(
        string tipo,
        string nombrePaciente,
        string especialidad,
        string medico,
        DateTime inicio,
        DateTime fin)
    {
        var cuando = $"{inicio:yyyy-MM-dd HH:mm} a {fin:HH:mm} (hora de Guatemala)";

        return tipo switch
        {
            NotificacionTipos.SolicitudRecibida => (
                "Clínica Pro — recibimos su solicitud de cita",
                $"Hola {nombrePaciente},\n\nRecibimos su solicitud de {especialidad} con {medico}.\nHorario solicitado: {cuando}.\n\nRecepción revisará la solicitud y le avisaremos cuando quede programada."),
            NotificacionTipos.CitaProgramada => (
                "Clínica Pro — su cita fue programada",
                $"Hola {nombrePaciente},\n\nSu cita de {especialidad} con {medico} quedó programada.\nFecha y hora: {cuando}.\n\nConfirme su asistencia desde la aplicación. Si no puede ir, cancele con anticipación o llame a recepción."),
            NotificacionTipos.CitaConfirmada => (
                "Clínica Pro — asistencia confirmada",
                $"Hola {nombrePaciente},\n\nQuedó confirmada su asistencia a {especialidad} con {medico}.\nFecha y hora: {cuando}.\n\nPreséntese en recepción a esa hora."),
            NotificacionTipos.CitaRechazada => (
                "Clínica Pro — no fue posible programar su cita",
                $"Hola {nombrePaciente},\n\nLa solicitud de {especialidad} para {cuando} no pudo programarse.\nPuede pedir otro horario desde la aplicación o llamar a recepción."),
            NotificacionTipos.CitaCancelada => (
                "Clínica Pro — su cita fue cancelada",
                $"Hola {nombrePaciente},\n\nLa cita de {especialidad} con {medico} ({cuando}) fue cancelada.\nSi desea otro espacio, solicite una nueva cita."),
            NotificacionTipos.CitaReprogramada => (
                "Clínica Pro — su cita fue reprogramada",
                $"Hola {nombrePaciente},\n\nSu cita de {especialidad} con {medico} cambió de horario.\nNueva fecha y hora: {cuando}.\n\nSi no puede asistir, cancele con anticipación o llame a recepción."),
            NotificacionTipos.CitaNoPresentada => (
                "Clínica Pro — inasistencia registrada",
                $"Hola {nombrePaciente},\n\nLa cita de {especialidad} con {medico} ({cuando}) quedó como no presentada.\nSi fue un error, comuníquese con recepción."),
            NotificacionTipos.RecordatorioCita => (
                "Clínica Pro — recordatorio de su cita",
                $"Hola {nombrePaciente},\n\nLe recordamos su cita de {especialidad} con {medico}.\nFecha y hora: {cuando}."),
            _ => (
                "Clínica Pro — actualización de cita",
                $"Hola {nombrePaciente},\n\nHay una actualización de su cita de {especialidad} ({cuando}).")
        };
    }
}
