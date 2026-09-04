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
    IAutorizacionReprogramacionRepository autorizaciones,
    ValidarAgendaPacienteService validarAgendaPaciente,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion,
    AjustarRecordatorioCitaService ajustarRecordatorio)
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
        Guid? autorizacion = esAdministrador ? usuarioId : null;
        AutorizacionReprogramacion? aprobada = null;
        if (autorizacion is null && cita.NumeroReprogramaciones + 1 >= Cita.MaximoReprogramaciones)
        {
            aprobada = await autorizaciones.ObtenerAprobadaPorCitaAsync(cita.Id, cancellationToken)
                ?? throw new DomainException("La tercera reprogramación requiere autorización de un Administrador.");
            autorizacion = aprobada.AutorizadaPorUsuarioId;
            aprobada.MarcarUsada();
        }

        cita.Reprogramar(nuevaFechaHoraInicio, duracion, autorizacion);

        await validarAgendaPaciente.ExigirPuedeAgendarAsync(
            cita.PacienteId,
            cita.FechaHoraInicio,
            cita.FechaHoraFin,
            cita.Id,
            cuentaComoCitaNueva: false,
            cancellationToken);

        var motivoFinal = string.IsNullOrWhiteSpace(motivo)
            ? "Reprogramación de la cita"
            : motivo.Trim();
        if (aprobada is not null)
        {
            motivoFinal = $"{motivoFinal}. Autorizó administrador {aprobada.AutorizadaPorUsuarioId}.";
        }

        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivoFinal, cancellationToken);
        await ajustarRecordatorio.AnularPendientesAsync(cita.Id, "Cita reprogramada.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken, NotificacionTipos.CitaReprogramada);
        return cita;
    }
}

public sealed class CancelarCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IParametroRepository parametros,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion,
    AjustarRecordatorioCitaService ajustarRecordatorio)
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
            ParametrosClave.HorasMinimasCancelacion,
            2,
            cancellationToken);

        cita.Cancelar(HoraClinica.Ahora(), horas);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        await ajustarRecordatorio.AnularPendientesAsync(cita.Id, "Cita cancelada.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }
}
