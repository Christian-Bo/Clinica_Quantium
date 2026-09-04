using ClinicaPro.Application.Auth;
using ClinicaPro.Contracts.Auth;

namespace ClinicaPro.Api.Security;

public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var usuario = context.User;
        if (usuario.Identity?.IsAuthenticated == true
            && usuario.HasClaim(MustChangePasswordAccess.ClaimType, MustChangePasswordAccess.ClaimValue)
            && !MustChangePasswordAccess.Permite(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ErrorResponse("Debe cambiar su contraseña antes de continuar."));
            return;
        }

        await next(context);
    }
}
