using ClinicaPro.Contracts.Auth;
using ClinicaPro.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace ClinicaPro.Api;

internal sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ForbiddenException forbiddenException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await httpContext.Response.WriteAsJsonAsync(
                new ErrorResponse(forbiddenException.Message),
                cancellationToken);
            return true;
        }

        if (exception is not DomainException domainException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(domainException.Message),
            cancellationToken);
        return true;
    }
}
