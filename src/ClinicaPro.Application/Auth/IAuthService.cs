namespace ClinicaPro.Application.Auth;

public sealed record RegisterPacienteInput(
    string Email,
    string Password,
    string Nombres,
    string Apellidos,
    string? Documento,
    string? Telefono,
    string? Direccion,
    DateOnly? FechaNacimiento);

public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    Guid UsuarioId,
    string Email,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    Guid? PacienteId);

public sealed record AuthUserInfo(
    Guid UsuarioId,
    string Email,
    IReadOnlyList<string> Roles,
    bool MustChangePassword,
    Guid? PacienteId,
    string? NombreCompleto);

public sealed record AuthOperationResult(bool Succeeded, string? ErrorCode, AuthSession? Session)
{
    public static AuthOperationResult Ok(AuthSession session) => new(true, null, session);

    public static AuthOperationResult Fail(string errorCode) => new(false, errorCode, null);
}

public sealed record PacienteStaffResult(bool Succeeded, string? ErrorCode, Guid? PacienteId, Guid? UsuarioId);

public interface IAuthService
{
    Task<AuthOperationResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ChangePasswordAsync(
        Guid usuarioId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
    Task<AuthOperationResult> RegisterPacienteAsync(RegisterPacienteInput input, CancellationToken cancellationToken = default);
    Task<PacienteStaffResult> RegisterPacientePorStaffAsync(RegisterPacienteInput input, CancellationToken cancellationToken = default);
    Task<AuthUserInfo?> ObtenerUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
