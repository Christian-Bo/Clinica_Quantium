using System.Security.Claims;
using ClinicaPro.Client.Shared.Constants;

namespace ClinicaPro.Client.Shared.Auth;

public static class RoleRoutes
{
    public static string Inicio(ClaimsPrincipal usuario)
    {
        if (usuario.IsInRole(Roles.Administrador)) return "/admin";
        if (usuario.IsInRole(Roles.Medico)) return "/medico/agenda";
        if (usuario.IsInRole(Roles.Secretaria)) return "/dashboard";
        return "/login";
    }

    public static string Inicio(IEnumerable<string> roles)
    {
        var lista = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (lista.Contains(Roles.Administrador)) return "/admin";
        if (lista.Contains(Roles.Medico)) return "/medico/agenda";
        if (lista.Contains(Roles.Secretaria)) return "/dashboard";
        return "/login";
    }
}
