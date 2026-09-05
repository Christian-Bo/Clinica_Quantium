using System.Security.Claims;
using ClinicaPro.Client.Shared.Constants;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClinicaPro.Client.Shared.Auth;

public enum PermisoUi
{
    VerAgendaSecretaria,
    GestionarPacientes,
    GestionarCitas,
    VerReportes,
    VerAgendaMedico,
    AtenderCitas,
    GestionarMedicos,
    GestionarUsuarios,
    ResolverAutorizaciones,
    GestionarParametros,
    VerAuditoria
}

/// <summary>
/// Matriz de capacidades de presentación. No reemplaza la autorización de la API;
/// evita que la UI repita comparaciones de roles en cada componente.
/// </summary>
public sealed class UiPermissionService(AuthenticationStateProvider authStateProvider)
{
    public async Task<bool> PuedeAsync(PermisoUi permiso)
    {
        var estado = await authStateProvider.GetAuthenticationStateAsync();
        return Puede(estado.User, permiso);
    }

    public bool Puede(ClaimsPrincipal usuario, PermisoUi permiso)
        => permiso switch
        {
            PermisoUi.VerAgendaSecretaria or PermisoUi.GestionarPacientes or PermisoUi.GestionarCitas
                => usuario.IsInRole(Roles.Secretaria),
            PermisoUi.VerAgendaMedico or PermisoUi.AtenderCitas
                => usuario.IsInRole(Roles.Medico),
            PermisoUi.GestionarMedicos or PermisoUi.GestionarUsuarios or PermisoUi.ResolverAutorizaciones
                or PermisoUi.GestionarParametros or PermisoUi.VerAuditoria
                => usuario.IsInRole(Roles.Administrador),
            PermisoUi.VerReportes
                => usuario.IsInRole(Roles.Secretaria) || usuario.IsInRole(Roles.Administrador),
            _ => false
        };
}
