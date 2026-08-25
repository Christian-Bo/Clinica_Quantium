using System.Security.Claims;

namespace ClinicaPro.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? ObtenerUsuarioId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");

        return Guid.TryParse(value, out var usuarioId) ? usuarioId : null;
    }
}
