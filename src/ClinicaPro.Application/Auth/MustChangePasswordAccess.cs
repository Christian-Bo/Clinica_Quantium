namespace ClinicaPro.Application.Auth;

public static class MustChangePasswordAccess
{
    public const string ClaimType = "must_change_password";
    public const string ClaimValue = "true";

    public static bool Permite(string path)
    {
        var ruta = (path ?? string.Empty).TrimEnd('/');
        return ruta.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
            || ruta.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase);
    }
}
