using System.Security.Claims;
using ClinicaPro.Client.Shared.Constants;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Única fuente de verdad de a dónde entra cada rol después de iniciar sesión.
/// El paciente va a su portal; el resto del personal al panel interno.
/// </summary>
public static class RutasPorRol
{
    public const string PortalPaciente = "/portal";
    public const string PanelPersonal = "/dashboard";
    public const string CambioPasswordObligatorio = "/cambiar-password";

    public static string InicioPara(IEnumerable<string> roles)
        => roles.Any(rol => string.Equals(rol, RolNombres.Paciente, StringComparison.OrdinalIgnoreCase))
            ? PortalPaciente
            : PanelPersonal;

    public static string InicioPara(ClaimsPrincipal usuario)
        => usuario.IsInRole(RolNombres.Paciente) ? PortalPaciente : PanelPersonal;
}
