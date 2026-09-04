using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed record SolicitarCitaInput(Guid MedicoId, DateTime FechaHoraInicio, string MotivoConsulta);

public sealed class SolicitarCitaService(
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    ICitaRepository citas,
    IParametroRepository parametros,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion)
{
    private async Task<Medico> ResolverMedicoAsync(
        SolicitarCitaInput input,
        CancellationToken cancellationToken)
    {
        return await medicos.ObtenerPorIdAsync(input.MedicoId, cancellationToken)
            ?? throw new DomainException("El médico seleccionado no existe o no está activo.");
    }

    public async Task<Cita> ExecuteAsync(
        Guid usuarioId,
        SolicitarCitaInput input,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        var medico = await ResolverMedicoAsync(input, cancellationToken);

        var duracion = await parametros.ObtenerEnteroAsync(
            "Citas.DuracionPredeterminadaMinutos",
            Cita.DuracionPredeterminadaMinutos,
            cancellationToken);

        var cita = Cita.Solicitar(
            paciente.Id,
            medico.Id,
            usuarioId,
            input.FechaHoraInicio,
            input.MotivoConsulta,
            duracion);

        await citas.AgregarAsync(cita, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }

    public async Task<Cita> ExecuteParaPacienteAsync(
        Guid staffUsuarioId,
        Guid pacienteId,
        SolicitarCitaInput input,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorIdAsync(pacienteId, cancellationToken)
            ?? throw new DomainException("El paciente no existe.");

        var medico = await ResolverMedicoAsync(input, cancellationToken);

        var duracion = await parametros.ObtenerEnteroAsync(
            "Citas.DuracionPredeterminadaMinutos",
            Cita.DuracionPredeterminadaMinutos,
            cancellationToken);

        var cita = Cita.Solicitar(
            paciente.Id,
            medico.Id,
            staffUsuarioId,
            input.FechaHoraInicio,
            input.MotivoConsulta,
            duracion);

        await citas.AgregarAsync(cita, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }
}

public sealed class OperarCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    IUnitOfWork unitOfWork,
    EncolarNotificacionCitaService encolarNotificacion,
    IAvisoTiempoRealAgenda avisoAgenda,
    AjustarRecordatorioCitaService ajustarRecordatorio)
{
    public async Task<Cita> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        cambiar(cita);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        await AvisarLlegadaSiAplicaAsync(cita, cancellationToken);
        await AnularRecordatorioSiCierraAsync(cita, cancellationToken);
        return cita;
    }

    private async Task AnularRecordatorioSiCierraAsync(Cita cita, CancellationToken cancellationToken)
    {
        if (cita.Estado is not (
            CitaEstados.Cancelada or CitaEstados.Rechazada or CitaEstados.NoPresentada or CitaEstados.Atendida))
        {
            return;
        }

        await ajustarRecordatorio.AnularPendientesAsync(cita.Id, $"Cita {cita.Estado}.", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task AvisarLlegadaSiAplicaAsync(Cita cita, CancellationToken cancellationToken)
    {
        if (cita.Estado != CitaEstados.EnEspera)
        {
            return;
        }

        var paciente = await pacientes.ObtenerPorIdAsync(cita.PacienteId, cancellationToken);
        var medico = await medicos.ObtenerPorIdAsync(cita.MedicoId, cancellationToken);
        if (medico is null)
        {
            return;
        }

        try
        {
            await avisoAgenda.PacienteLlegoAsync(
                medico.UsuarioId,
                new PacienteLlegoAviso(
                    cita.Id,
                    cita.PacienteId,
                    paciente?.NombreCompleto ?? "Paciente",
                    cita.FechaHoraInicio),
                cancellationToken);
        }
        catch
        {
            // La llegada ya quedó guardada; el aviso en vivo no debe revertirla.
        }
    }

    public async Task<Cita> ExecuteComoMedicoOAdminAsync(
        Guid citaId,
        Guid usuarioId,
        bool esAdministrador,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (!esAdministrador)
        {
            var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
                ?? throw new DomainException("El usuario no tiene un perfil de médico.");

            CitaAccesoMedico.ExigirAsignado(cita, medico.Id);
        }

        cambiar(cita);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }

    public async Task<Cita> ExecuteComoPacienteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (cita.PacienteId != paciente.Id)
        {
            throw new DomainException("La cita no pertenece al paciente autenticado.");
        }

        cambiar(cita);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        await encolarNotificacion.ExecuteAsync(cita, cancellationToken);
        return cita;
    }
}

public sealed class ListarCitasPacienteService(IPacienteRepository pacientes, ICitaRepository citas)
{
    public async Task<IReadOnlyList<Cita>> ExecuteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        return await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken);
    }
}

public sealed class ListarCitasMedicoService(IMedicoRepository medicos, ICitaRepository citas)
{
    public async Task<IReadOnlyList<Cita>> ExecuteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de médico.");

        return await citas.ListarPorMedicoAsync(medico.Id, cancellationToken);
    }
}

public sealed class ListarCitasPorPacienteStaffService(IPacienteRepository pacientes, ICitaRepository citas)
{
    public async Task<IReadOnlyList<Cita>?> ExecuteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return null;
        }

        return await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken);
    }
}

public sealed class ListarCitasPendientesService(ICitaRepository citas)
{
    public Task<IReadOnlyList<Cita>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return citas.ListarPorEstadoAsync(CitaEstados.Solicitada, cancellationToken);
    }
}

public sealed class ListarAgendaService(ICitaRepository citas, IMedicoRepository medicos)
{
    public async Task<IReadOnlyList<Cita>> ExecuteAsync(
        DateTime? desde,
        DateTime? hasta,
        Guid? medicoId,
        Guid usuarioId,
        bool soloAgendaPropia,
        CancellationToken cancellationToken = default)
    {
        var inicio = DateTime.SpecifyKind(desde ?? DateTime.Today, DateTimeKind.Unspecified);
        var fin = DateTime.SpecifyKind(hasta ?? inicio.AddDays(7), DateTimeKind.Unspecified);
        if (fin <= inicio)
        {
            throw new DomainException("El rango de agenda es inválido.");
        }

        Guid? filtroMedicoId = medicoId;
        if (soloAgendaPropia)
        {
            var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
                ?? throw new DomainException("El usuario no tiene un perfil de médico.");
            filtroMedicoId = medico.Id;
        }

        return await citas.ListarEnRangoAsync(inicio, fin, filtroMedicoId, cancellationToken);
    }
}
