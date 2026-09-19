using ClinicaPro.Api.Security;
using ClinicaPro.Application.Auth;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Contracts.Auth;
using ClinicaPro.Contracts.Pacientes;
using ClinicaPro.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
public sealed class PacientesController(
    IPacienteRepository pacienteRepository,
    IAuthService authService,
    BuscarPacientesService buscarPacientes,
    ActualizarPerfilPacienteService actualizarPerfil,
    DarDeBajaPacientePorInasistenciaService darDeBaja,
    ObtenerExpedientePacienteService obtenerExpediente) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PacienteDto>> Me(CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var paciente = await pacienteRepository.ObtenerPorUsuarioIdAsync(usuarioId.Value, cancellationToken);
        if (paciente is null)
        {
            return NotFound();
        }

        return Ok(Map(paciente));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet]
    [ProducesResponseType(typeof(PaginaPacientesDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginaPacientesDto>> Buscar(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = BuscarPacientesService.PageSizePorDefecto,
        CancellationToken cancellationToken = default)
    {
        var resultado = await buscarPacientes.ExecuteAsync(q, page, pageSize, cancellationToken);
        return Ok(new PaginaPacientesDto(
            resultado.Items.Select(Map).ToList(),
            resultado.Total,
            resultado.Page,
            resultado.PageSize));
    }

    [Authorize(Roles = RolNombres.Paciente)]
    [HttpPut("me")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PacienteDto>> ActualizarMe(
        [FromBody] ActualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var paciente = await actualizarPerfil.ExecuteAsync(
            usuarioId.Value,
            request.Nombres,
            request.Apellidos,
            request.Documento,
            request.FechaNacimiento,
            request.Telefono,
            request.Direccion,
            request.Sexo,
            request.Alergias,
            request.ContactoEmergenciaNombre,
            request.ContactoEmergenciaTelefono,
            cancellationToken);

        return Ok(Map(paciente));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPut("{pacienteId:guid}")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PacienteDto>> Actualizar(
        Guid pacienteId,
        [FromBody] ActualizarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var paciente = await actualizarPerfil.ExecutePorPacienteIdAsync(
            usuarioId.Value,
            pacienteId,
            request.Nombres,
            request.Apellidos,
            request.Documento,
            request.FechaNacimiento,
            request.Telefono,
            request.Direccion,
            request.Sexo,
            request.Alergias,
            request.ContactoEmergenciaNombre,
            request.ContactoEmergenciaTelefono,
            cancellationToken);

        return Ok(Map(paciente));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PacienteDto>> Crear(
        [FromBody] RegisterPacienteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse("El correo y la contraseña temporal son obligatorios."));
        }

        var resultado = await authService.RegisterPacientePorStaffAsync(
            new RegisterPacienteInput(
                request.Email,
                request.Password,
                request.Nombres,
                request.Apellidos,
                request.Documento,
                request.Telefono,
                request.Direccion,
                request.FechaNacimiento,
                request.Sexo,
                request.Alergias,
                request.ContactoEmergenciaNombre,
                request.ContactoEmergenciaTelefono),
            cancellationToken);

        if (!resultado.Succeeded || resultado.PacienteId is null)
        {
            return resultado.ErrorCode switch
            {
                "email_taken" => Conflict(new ErrorResponse("El correo ya está registrado.")),
                "documento_taken" => Conflict(new ErrorResponse("Ya existe un paciente con ese documento.")),
                _ => BadRequest(new ErrorResponse("No fue posible registrar al paciente. Revise nombres, apellidos y contraseña."))
            };
        }

        var paciente = await pacienteRepository.ObtenerPorIdAsync(resultado.PacienteId.Value, cancellationToken);
        if (paciente is null)
        {
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "El paciente se creó pero no pudo leerse.");
        }

        return Created("/api/pacientes/me", Map(paciente));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador + "," + RolNombres.Medico)]
    [HttpGet("{pacienteId:guid}/expediente")]
    [ProducesResponseType(typeof(ExpedientePacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpedientePacienteDto>> Expediente(
        Guid pacienteId,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var resultado = await obtenerExpediente.ExecuteAsync(
            pacienteId,
            usuarioId.Value,
            User.IsInRole(RolNombres.Secretaria) || User.IsInRole(RolNombres.Administrador),
            cancellationToken);
        if (resultado is null)
        {
            return NotFound();
        }

        return Ok(new ExpedientePacienteDto(
            new PacienteContextoMedicoDto(
                resultado.Paciente.Id,
                resultado.Paciente.NombreCompleto,
                resultado.Paciente.Sexo,
                resultado.Paciente.Alergias,
                resultado.Paciente.FechaNacimiento,
                resultado.Paciente.Telefono),
            resultado.Visitas.Select(visita => new ExpedienteVisitaResumenDto(
                visita.Cita.Id,
                visita.Cita.FechaHoraInicio,
                visita.Cita.FechaHoraFin,
                visita.Cita.Estado,
                visita.Cita.MedicoId,
                visita.MedicoNombre,
                visita.Cita.MotivoConsulta,
                visita.Preconsulta is not null,
                visita.CantidadIris,
                visita.Preconsulta?.PresionSistolicaMmHg,
                visita.Preconsulta?.PresionDiastolicaMmHg,
                visita.Preconsulta?.TemperaturaCelsius,
                visita.Preconsulta?.OxigenoSangrePorcentaje)).ToList()));
    }

    [Authorize(Roles = RolNombres.Administrador)]
    [HttpDelete("{pacienteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DarDeBajaPorInasistencia(
        Guid pacienteId,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        await darDeBaja.ExecuteAsync(pacienteId, usuarioId.Value, cancellationToken);
        return NoContent();
    }

    private static PacienteDto Map(ClinicaPro.Domain.Entities.Paciente paciente) => new(
        paciente.Id,
        paciente.UsuarioId,
        paciente.Nombres,
        paciente.Apellidos,
        paciente.NombreCompleto,
        paciente.Documento,
        paciente.FechaNacimiento,
        paciente.Telefono,
        paciente.Direccion,
        paciente.Sexo,
        paciente.Alergias,
        paciente.ContactoEmergenciaNombre,
        paciente.ContactoEmergenciaTelefono);
}
