using ClinicaPro.Application.Especialidades;
using ClinicaPro.Contracts.Especialidades;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Route("api/especialidades")]
public sealed class EspecialidadesController(ListarEspecialidadesActivasService listarEspecialidades)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EspecialidadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EspecialidadDto>>> Get(
        CancellationToken cancellationToken)
    {
        var especialidades = await listarEspecialidades.ExecuteAsync(cancellationToken);

        var respuesta = especialidades
            .Select(especialidad => new EspecialidadDto(
                especialidad.Id,
                especialidad.Nombre,
                especialidad.Descripcion))
            .ToList();

        return Ok(respuesta);
    }
}
