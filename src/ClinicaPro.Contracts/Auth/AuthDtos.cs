namespace ClinicaPro.Contracts.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record RegisterPacienteRequest(
    string Email,
    string Password,
    string Nombres,
    string Apellidos,
    string? Documento,
    string? Telefono,
    string? Direccion,
    DateOnly? FechaNacimiento);

public sealed record ActualizarPerfilRequest(
    string Nombres,
    string Apellidos,
    string? Documento,
    string? Telefono,
    string? Direccion,
    DateOnly? FechaNacimiento);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UsuarioId,
    string Email,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    Guid? PacienteId);

public sealed record UsuarioActualDto(
    Guid UsuarioId,
    string Email,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    Guid? PacienteId,
    string? NombreCompleto);

public sealed record ErrorResponse(string Error);
