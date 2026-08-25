using ClinicaPro.Api.Security;
using ClinicaPro.Application.Auth;
using ClinicaPro.Application.Pacientes;
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
    IAuthService authService) : ControllerBase
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
                request.FechaNacimiento),
            cancellationToken);

        if (!resultado.Succeeded || resultado.PacienteId is null)
        {
            return resultado.ErrorCode switch
            {
                "email_taken" => Conflict(new ErrorResponse("El correo ya está registrado.")),
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

    private static PacienteDto Map(ClinicaPro.Domain.Entities.Paciente paciente) => new(
        paciente.Id,
        paciente.UsuarioId,
        paciente.Nombres,
        paciente.Apellidos,
        paciente.NombreCompleto,
        paciente.Documento,
        paciente.FechaNacimiento,
        paciente.Telefono,
        paciente.Direccion);
}
