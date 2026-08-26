using ClinicaPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public sealed class HealthController(
    ClinicaProDbContext dbContext,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            application = "ClinicaPro.Api",
            timestampUtc = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> Database(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Base de datos no disponible");
            }

            var conexion = dbContext.Database.GetDbConnection();

            return Ok(new
            {
                status = "ok",
                database = conexion.Database,
                server = conexion.DataSource,
                provider = dbContext.Database.ProviderName,
                timestampUtc = DateTimeOffset.UtcNow
            });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No fue posible conectar con la base de datos ClinicaPro.");

            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Base de datos no disponible");
        }
    }
}
