using System.Security.Claims;
using ClinicaPro.Application.Auth;
using ClinicaPro.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse("El correo y la contraseña son obligatorios."));
        }

        var result = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        return ToActionResult(result);
    }

    [AllowAnonymous]
    [HttpPost("register/paciente")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> RegisterPaciente(
        [FromBody] RegisterPacienteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse("El correo y la contraseña son obligatorios."));
        }

        var result = await authService.RegisterPacienteAsync(
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

        if (!result.Succeeded || result.Session is null)
        {
            return ToActionResult(result);
        }

        return Created("/api/auth/me", Map(result.Session));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioActualDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioActualDto>> Me(CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var usuario = await authService.ObtenerUsuarioAsync(usuarioId.Value, cancellationToken);
        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(new UsuarioActualDto(
            usuario.UsuarioId,
            usuario.Email,
            usuario.Roles,
            usuario.MustChangePassword,
            usuario.PacienteId,
            usuario.NombreCompleto));
    }

    private ActionResult<AuthResponse> ToActionResult(AuthOperationResult result)
    {
        if (result.Succeeded && result.Session is not null)
        {
            return Ok(Map(result.Session));
        }

        return result.ErrorCode switch
        {
            "locked_out" => StatusCode(StatusCodes.Status423Locked),
            "email_taken" => Conflict(new ErrorResponse("El correo ya está registrado.")),
            "invalid_patient" or "invalid_user" or "PasswordTooShort" or "PasswordRequiresDigit"
                or "PasswordRequiresUpper" or "PasswordRequiresLower" or "PasswordRequiresNonAlphanumeric"
                => BadRequest(new ErrorResponse("Los datos de registro no cumplen las reglas de usuario o contraseña.")),
            "role_missing" or "role_assignment_failed" => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "No fue posible asignar el rol de paciente."),
            _ => Unauthorized()
        };
    }

    private static AuthResponse Map(AuthSession session) => new(
        session.AccessToken,
        session.ExpiresAtUtc,
        session.UsuarioId,
        session.Email,
        session.Roles,
        session.MustChangePassword,
        session.PacienteId);

    private Guid? ObtenerUsuarioId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(value, out var usuarioId) ? usuarioId : null;
    }
}
