using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class ReprogramarCitaService(
    ICitaRepository citas,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion)
{
    public async Task<Cita> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        bool esAdministrador,
        DateTime nuevaFechaHoraInicio,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        var duracion = (int)Math.Round((cita.FechaHoraFin - cita.FechaHoraInicio).TotalMinutes);
        var autorizacion = esAdministrador ? usuarioId : (Guid?)null;
        cita.Reprogramar(nuevaFechaHoraInicio, duracion, autorizacion);

        var motivoFinal = string.IsNullOrWhiteSpace(motivo)
            ? "Reprogramación de la cita"
            : motivo.Trim();

        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivoFinal, cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken, NotificacionTipos.CitaReprogramada);
        return cita;
    }
}

public sealed class CancelarCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IParametroRepository parametros,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion)
{
    public Task<Cita> ExecuteComoStaffAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        return CancelarAsync(citaId, usuarioId, motivo, pacienteId: null, cancellationToken);
    }

    public async Task<Cita> ExecuteComoPacienteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        return await CancelarAsync(citaId, usuarioId, motivo, paciente.Id, cancellationToken);
    }

    private async Task<Cita> CancelarAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        Guid? pacienteId,
        CancellationToken cancellationToken)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (pacienteId is not null && cita.PacienteId != pacienteId)
        {
            throw new DomainException("La cita no pertenece al paciente autenticado.");
        }

        var horas = await parametros.ObtenerEnteroAsync(
            "Citas.HorasMinimasCancelacion",
            2,
            cancellationToken);

        cita.Cancelar(HoraClinica.Ahora(), horas);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }
}
