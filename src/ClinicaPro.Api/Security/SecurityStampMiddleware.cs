using ClinicaPro.Application.Auth;
using ClinicaPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace ClinicaPro.Api.Security;

public sealed class SecurityStampMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            var usuarioId = principal.ObtenerUsuarioId();
            var stamp = principal.FindFirst(SecurityStampAccess.ClaimType)?.Value;
            if (usuarioId is null || string.IsNullOrWhiteSpace(stamp))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var user = await userManager.FindByIdAsync(usuarioId.Value.ToString());
            if (user is null
                || !user.IsActive
                || !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}
