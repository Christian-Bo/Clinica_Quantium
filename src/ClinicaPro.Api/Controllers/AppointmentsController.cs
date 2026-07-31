using ClinicaPro.Application.Appointments;
using ClinicaPro.Contracts.Appointments;
using ClinicaPro.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    [HttpPost("requests")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AppointmentResponse>> RequestAppointment(
        RequestAppointmentRequest request,
        [FromServices] RequestAppointmentHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(request, cancellationToken);
            return Created($"api/appointments/{result.Id}", result);
        }
        catch (BusinessRuleException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "La solicitud no cumple las reglas de negocio.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Los datos de la solicitud no son válidos.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
