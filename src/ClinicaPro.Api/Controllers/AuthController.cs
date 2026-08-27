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
                request.FechaNacimiento,
                request.Sexo,
                request.Alergias,
                request.ContactoEmergenciaNombre,
                request.ContactoEmergenciaTelefono),
            cancellationToken);

        if (!result.Succeeded || result.Session is null)
        {
            return ToActionResult(result);
        }

        return Created("/api/auth/me", Map(result.Session));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ErrorResponse("El correo es obligatorio."));
        }

        await authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ErrorResponse("Correo, token y nueva contraseña son obligatorios."));
        }

        var result = await authService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok();
        }

        return result.ErrorCode is "PasswordTooShort" or "PasswordRequiresDigit"
            or "PasswordRequiresUpper" or "PasswordRequiresLower" or "PasswordRequiresNonAlphanumeric"
            ? BadRequest(new ErrorResponse("La nueva contraseña no cumple las reglas."))
            : BadRequest(new ErrorResponse("No fue posible restablecer la contraseña. Pida un código nuevo."));
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ErrorResponse("La contraseña actual y la nueva son obligatorias."));
        }

        var result = await authService.ChangePasswordAsync(
            usuarioId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return ToActionResult(result);
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
            "documento_taken" => Conflict(new ErrorResponse("Ya existe un paciente con ese documento.")),
            "password_mismatch" => BadRequest(new ErrorResponse("La contraseña actual no es correcta.")),
            "password_same" => BadRequest(new ErrorResponse("La nueva contraseña debe ser distinta a la actual.")),
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
