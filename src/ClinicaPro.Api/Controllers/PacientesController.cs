using System.Security.Claims;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Contracts.Pacientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/pacientes")]
public sealed class PacientesController(IPacienteRepository pacienteRepository) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(PacienteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PacienteDto>> Me(CancellationToken cancellationToken)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(value, out var usuarioId))
        {
            return Unauthorized();
        }

        var paciente = await pacienteRepository.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);
        if (paciente is null)
        {
            return NotFound();
        }

        return Ok(new PacienteDto(
            paciente.Id,
            paciente.UsuarioId,
            paciente.Nombres,
            paciente.Apellidos,
            paciente.NombreCompleto,
            paciente.Documento,
            paciente.FechaNacimiento,
            paciente.Telefono,
            paciente.Direccion));
    }
}
