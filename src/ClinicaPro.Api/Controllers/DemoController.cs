using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolNombres.Administrador)]
[Route("api/demo")]
public sealed class DemoController(IPrepararAgendaDemo prepararAgenda) : ControllerBase
{
    [HttpPost("preparar-agenda")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> PrepararAgenda(CancellationToken cancellationToken)
    {
        var mensaje = await prepararAgenda.ExecuteAsync(cancellationToken);
        return Ok(new { mensaje });
    }
}
