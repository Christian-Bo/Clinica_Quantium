using ClinicaPro.Api.Security;
using ClinicaPro.Application.Citas;
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
    ReprogramarCitaService reprogramarCita,
    CancelarCitaService cancelarCita,
    ListarCitasPacienteService listarCitasPaciente,
    ListarCitasMedicoService listarCitasMedico,
    ListarCitasPendientesService listarPendientes,
    ListarAgendaService listarAgenda,
    ListarCitasPorPacienteStaffService listarCitasPacienteStaff,
    ListarDisponibilidadService listarDisponibilidad,
    ListarHistorialCitaService listarHistorial,
    ResolverNombresCitaService resolverNombres) : ControllerBase
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

        return Created($"/api/citas/{cita.Id}", await MapAsync(cita, cancellationToken));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("para-paciente")]
    [ProducesResponseType(typeof(CitaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CitaDto>> SolicitarParaPaciente(
        [FromBody] SolicitarCitaParaPacienteRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var cita = await solicitarCita.ExecuteParaPacienteAsync(
            usuarioId.Value,
            request.PacienteId,
            new SolicitarCitaInput(request.EspecialidadId, request.FechaHoraInicio, request.MotivoConsulta),
            cancellationToken);

        return Created($"/api/citas/{cita.Id}", await MapAsync(cita, cancellationToken));
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
        return Ok(await MapManyAsync(citas, cancellationToken));
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
        return Ok(await MapManyAsync(citas, cancellationToken));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet("pendientes")]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> Pendientes(CancellationToken cancellationToken)
    {
        var citas = await listarPendientes.ExecuteAsync(cancellationToken);
        return Ok(await MapManyAsync(citas, cancellationToken));
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
        return Ok(await MapManyAsync(citas, cancellationToken));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CitaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CitaDto>>> PorPaciente(
        [FromQuery] Guid pacienteId,
        CancellationToken cancellationToken)
    {
        if (pacienteId == Guid.Empty)
        {
            return BadRequest();
        }

        var citas = await listarCitasPacienteStaff.ExecuteAsync(pacienteId, cancellationToken);
        if (citas is null)
        {
            return NotFound();
        }

        return Ok(await MapManyAsync(citas, cancellationToken));
    }

    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotDisponibleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotDisponibleDto>>> Disponibilidad(
        [FromQuery] Guid especialidadId,
        [FromQuery] DateOnly fecha,
        CancellationToken cancellationToken)
    {
        if (especialidadId == Guid.Empty)
        {
            return BadRequest();
        }

        var slots = await listarDisponibilidad.ExecuteAsync(especialidadId, fecha, cancellationToken);
        return Ok(slots.Select(slot => new SlotDisponibleDto(
            slot.FechaHoraInicio,
            slot.FechaHoraFin,
            slot.MedicoId,
            slot.MedicoNombre)).ToList());
    }

    [HttpGet("{citaId:guid}/historial")]
    [ProducesResponseType(typeof(IReadOnlyList<HistorialCitaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HistorialCitaDto>>> Historial(
        Guid citaId,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var items = await listarHistorial.ExecuteAsync(
            citaId,
            usuarioId.Value,
            User.ObtenerRoles(),
            cancellationToken);

        return Ok(items.Select(item => new HistorialCitaDto(
            item.HistorialCitaId,
            item.CitaId,
            item.UsuarioId,
            item.ActorNombre,
            item.ActorRol,
            item.TipoCambio,
            item.EstadoAnterior,
            item.EstadoNuevo,
            item.FechaHoraInicioAnterior,
            item.FechaHoraInicioNueva,
            item.FechaHoraFinAnterior,
            item.FechaHoraFinNueva,
            item.Motivo,
            item.FechaCambioUtc,
            item.FechaCambioLocal,
            item.Descripcion)).ToList());
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

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/reprogramar")]
    public async Task<ActionResult<CitaDto>> Reprogramar(
        Guid citaId,
        [FromBody] ReprogramarCitaRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var cita = await reprogramarCita.ExecuteAsync(
            citaId,
            usuarioId.Value,
            User.IsInRole(RolNombres.Administrador),
            request.FechaHoraInicio,
            request.Motivo ?? string.Empty,
            cancellationToken);

        return Ok(await MapAsync(cita, cancellationToken));
    }

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
    public async Task<ActionResult<CitaDto>> CancelarPaciente(
        Guid citaId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var motivo = string.IsNullOrWhiteSpace(request?.Motivo) ? "Cancelación del paciente" : request.Motivo.Trim();
        var cita = await cancelarCita.ExecuteComoPacienteAsync(citaId, usuarioId.Value, motivo, cancellationToken);
        return Ok(await MapAsync(cita, cancellationToken));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("{citaId:guid}/cancelar-administrativa")]
    public async Task<ActionResult<CitaDto>> CancelarAdministrativa(
        Guid citaId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var motivo = string.IsNullOrWhiteSpace(request?.Motivo)
            ? "Cancelación administrativa"
            : request.Motivo.Trim();
        var cita = await cancelarCita.ExecuteComoStaffAsync(citaId, usuarioId.Value, motivo, cancellationToken);
        return Ok(await MapAsync(cita, cancellationToken));
    }

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
        return Ok(await MapAsync(cita, cancellationToken));
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
        return Ok(await MapAsync(cita, cancellationToken));
    }

    private async Task<CitaDto> MapAsync(Cita cita, CancellationToken cancellationToken)
    {
        var lista = await MapManyAsync([cita], cancellationToken);
        return lista[0];
    }

    private async Task<List<CitaDto>> MapManyAsync(IReadOnlyList<Cita> citas, CancellationToken cancellationToken)
    {
        var nombres = await resolverNombres.ExecuteAsync(citas, cancellationToken);
        return citas.Select(cita =>
        {
            var extra = nombres.GetValueOrDefault(cita.Id, new NombresCita(string.Empty, string.Empty, string.Empty));
            return new CitaDto(
                cita.Id,
                cita.PacienteId,
                extra.PacienteNombre,
                cita.MedicoId,
                extra.MedicoNombre,
                cita.EspecialidadId,
                extra.EspecialidadNombre,
                cita.FechaHoraInicio,
                cita.FechaHoraFin,
                cita.MotivoConsulta,
                cita.Estado,
                cita.NumeroReprogramaciones);
        }).ToList();
    }
}
