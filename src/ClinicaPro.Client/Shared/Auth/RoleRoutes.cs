using System.Security.Claims;
using ClinicaPro.Client.Shared.Constants;

namespace ClinicaPro.Client.Shared.Auth;

public static class RoleRoutes
{
    /// <summary>Pantalla a la que se envía a un usuario obligado a cambiar su contraseña.</summary>
    public const string CambioContrasenaObligatorio = "/cambiar-contrasena";

    /// <summary>Inicio del portal del paciente.</summary>
    public const string PortalPaciente = "/portal";

    public static string Inicio(ClaimsPrincipal usuario)
    {
        if (usuario.IsInRole(Roles.Administrador)) return "/admin";
        if (usuario.IsInRole(Roles.Medico)) return "/medico/agenda";
        if (usuario.IsInRole(Roles.Secretaria)) return "/dashboard";
        if (usuario.IsInRole(Roles.Paciente)) return PortalPaciente;
        return "/login";
    }

    public static string Inicio(IEnumerable<string> roles)
    {
        var lista = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (lista.Contains(Roles.Administrador)) return "/admin";
        if (lista.Contains(Roles.Medico)) return "/medico/agenda";
        if (lista.Contains(Roles.Secretaria)) return "/dashboard";
        if (lista.Contains(Roles.Paciente)) return PortalPaciente;
        return "/login";
    }
}
