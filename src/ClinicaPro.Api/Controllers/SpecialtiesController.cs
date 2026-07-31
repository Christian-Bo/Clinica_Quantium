using ClinicaPro.Application.Catalogs;
using ClinicaPro.Contracts.Catalogs;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Route("api/specialties")]
public sealed class SpecialtiesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<SpecialtyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SpecialtyResponse>>> Get(
        [FromServices] GetSpecialtiesHandler handler,
        CancellationToken cancellationToken)
    {
        var specialties = await handler.HandleAsync(cancellationToken);
        return Ok(specialties);
    }
}
