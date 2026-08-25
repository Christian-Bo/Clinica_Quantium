using ClinicaPro.Api.Security;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/citas")]
public sealed class CitasController(
    SolicitarCitaService solicitarCita,
    OperarCitaService operarCita,
    ListarCitasPacienteService listarCitasPaciente,
    ListarCitasMedicoService listarCitasMedico,
    ListarCitasPendientesService listarPendientes,
    ListarAgendaService listarAgenda,
    IHistorialCitaRepository historial) : ControllerBase
{
    [Authorize(Roles = RolNombres.Paciente)]
    [HttpPost]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CitaDto>> Solicitar(
        [FromBody] SolicitarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var cita = await solicitarCita.ExecuteAsync(
            usuarioId.Value,
            new SolicitarCitaInput(request.EspecialidadId, request.FechaHoraInicio, request.MotivoConsulta),
            cancellationToken);

        return Created($"/api/citas/{cita.Id}", Map(cita));
    }

    [Authorize(Roles = RolNombres.Paciente)]
    [HttpGet("mias")]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> Mias(CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var citas = await listarCitasPaciente.ExecuteAsync(usuarioId.Value, cancellationToken);
        return Ok(citas.Select(Map).ToList());
    }

    [Authorize(Roles = RolNombres.Medico)]
    [HttpGet("medico")]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> DelMedico(CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var citas = await listarCitasMedico.ExecuteAsync(usuarioId.Value, cancellationToken);
        return Ok(citas.Select(Map).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet("pendientes")]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> Pendientes(CancellationToken cancellationToken)
    {
        var citas = await listarPendientes.ExecuteAsync(cancellationToken);
        return Ok(citas.Select(Map).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador + "," + RolNombres.Medico)]
    [HttpGet("agenda")]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> Agenda(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] Guid? medicoId,
        CancellationToken cancellationToken)
    {
        var citas = await listarAgenda.ExecuteAsync(desde, hasta, medicoId, cancellationToken);
        return Ok(citas.Select(Map).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet("{citaId:guid}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialCitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HistorialCitaDto>>> Historial(
        Guid citaId,
        CancellationToken cancellationToken)
    {
        var items = await historial.ListarPorCitaAsync(citaId, cancellationToken);
        return Ok(items.Select(item => new HistorialCitaDto(
            item.Id,
            item.CitaId,
            item.UsuarioId,
            item.TipoCambio,
            item.EstadoAnterior,
            item.EstadoNuevo,
            item.Motivo,
            item.FechaCambioUtc)).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/confirmar")]
    public Task<ActionResult<CitaDto>> Confirmar(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoStaff(citaId, "Confirmación administrativa de la solicitud", cita => cita.ConfirmarPorSecretaria(User.ObtenerUsuarioId()!.Value), cancellationToken);

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/rechazar")]
    public Task<ActionResult<CitaDto>> Rechazar(
        Guid citaId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
        => CambiarComoStaff(
            citaId,
            string.IsNullOrWhiteSpace(request?.Motivo) ? "Rechazo administrativo de la solicitud" : request.Motivo.Trim(),
            cita => cita.RechazarPorSecretaria(User.ObtenerUsuarioId()!.Value),
            cancellationToken);

    [Authorize(Roles = RolNombres.Paciente)]
    [HttpPost("{citaId:guid}/anular-solicitud")]
    public Task<ActionResult<CitaDto>> AnularSolicitud(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoPaciente(citaId, "El paciente anuló la solicitud", cita => cita.CancelarSolicitudPorPaciente(), cancellationToken);

    [Authorize(Roles = RolNombres.Paciente)]
    [HttpPost("{citaId:guid}/confirmar-asistencia")]
    public Task<ActionResult<CitaDto>> ConfirmarAsistencia(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoPaciente(citaId, "El paciente confirmó su asistencia", cita => cita.ConfirmarAsistencia(), cancellationToken);

    [Authorize(Roles = RolNombres.Paciente)]
    [HttpPost("{citaId:guid}/cancelar")]
    public Task<ActionResult<CitaDto>> CancelarPaciente(
        Guid citaId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
        => CambiarComoPaciente(
            citaId,
            string.IsNullOrWhiteSpace(request?.Motivo) ? "Cancelación del paciente" : request.Motivo.Trim(),
            cita => cita.Cancelar(),
            cancellationToken);

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/cancelar-administrativa")]
    public Task<ActionResult<CitaDto>> CancelarAdministrativa(
        Guid citaId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
        => CambiarComoStaff(
            citaId,
            string.IsNullOrWhiteSpace(request?.Motivo) ? "Cancelación administrativa" : request.Motivo.Trim(),
            cita => cita.Cancelar(),
            cancellationToken);

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/llegada")]
    public Task<ActionResult<CitaDto>> Llegada(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoStaff(citaId, "El paciente llegó a recepción", cita => cita.RegistrarLlegada(), cancellationToken);

    [Authorize(Roles = RolNombres.Medico + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/iniciar")]
    public Task<ActionResult<CitaDto>> Iniciar(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoStaff(citaId, "Inicio de atención médica", cita => cita.IniciarAtencion(), cancellationToken);

    [Authorize(Roles = RolNombres.Medico + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/finalizar")]
    public Task<ActionResult<CitaDto>> Finalizar(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoStaff(citaId, "Consulta finalizada", cita => cita.FinalizarAtencion(), cancellationToken);

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/no-presentada")]
    public Task<ActionResult<CitaDto>> NoPresentada(Guid citaId, CancellationToken cancellationToken)
        => CambiarComoStaff(citaId, "El paciente no se presentó", cita => cita.MarcarNoPresentada(), cancellationToken);

    private async Task<ActionResult<CitaDto>> CambiarComoStaff(
        Guid citaId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var cita = await operarCita.ExecuteAsync(citaId, usuarioId.Value, motivo, cambiar, cancellationToken);
        return Ok(Map(cita));
    }

    private async Task<ActionResult<CitaDto>> CambiarComoPaciente(
        Guid citaId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var cita = await operarCita.ExecuteComoPacienteAsync(citaId, usuarioId.Value, motivo, cambiar, cancellationToken);
        return Ok(Map(cita));
    }

    private static CitaDto Map(Cita cita) => new(
        cita.Id,
        cita.PacienteId,
        cita.MedicoId,
        cita.EspecialidadId,
        cita.FechaHoraInicio,
        cita.FechaHoraFin,
        cita.MotivoConsulta,
        cita.Estado);
}
