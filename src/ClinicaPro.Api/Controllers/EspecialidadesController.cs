using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Contracts.Especialidades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/especialidades")]
public sealed class EspecialidadesController(
    ListarEspecialidadesActivasService listarEspecialidades,
    IMedicoRepository medicos)
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

    [HttpGet("{especialidadId:guid}/medico-primario")]
    [ProducesResponseType(typeof(MedicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicoDto>> MedicoPrimario(
        Guid especialidadId,
        CancellationToken cancellationToken)
    {
        var medico = await medicos.ObtenerPrimarioPorEspecialidadAsync(especialidadId, cancellationToken);
        if (medico is null)
        {
            return NotFound();
        }

        var especialidades = (await medicos.ListarEspecialidadesActivasAsync(cancellationToken))
            .Where(relacion => relacion.MedicoId == medico.Id)
            .ToList();

        return Ok(new MedicoDto(
            medico.Id,
            medico.Nombres,
            medico.Apellidos,
            medico.NombreCompleto,
            medico.NumeroColegiado,
            medico.Telefono,
            especialidades.Select(relacion => relacion.EspecialidadId).ToList(),
            especialidades.FirstOrDefault(relacion => relacion.EsPrimario)?.EspecialidadId));
    }
}
