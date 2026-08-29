using ClinicaPro.Application.Agenda;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/parametros")]
public sealed class ParametrosController(IParametroRepository parametros) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ParametroDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ParametroDto>>> Get(CancellationToken cancellationToken)
    {
        var lista = await parametros.ListarActivosAsync(cancellationToken);
        return Ok(lista
            .Where(parametro => parametro.Clave != ParametrosClave.MaximoReprogramaciones)
            .Select(parametro => new ParametroDto(
                parametro.Clave,
                parametro.Valor,
                parametro.TipoDato,
                parametro.Descripcion)).ToList());
    }
}
