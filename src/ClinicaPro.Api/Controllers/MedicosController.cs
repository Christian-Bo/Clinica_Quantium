using ClinicaPro.Application.Agenda;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/medicos")]
public sealed class MedicosController(
    ListarMedicosActivosService listarMedicos,
    IMedicoRepository medicos,
    IHorarioRepository horarios) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicoDto>>> Get(CancellationToken cancellationToken)
    {
        var lista = await listarMedicos.ExecuteAsync(cancellationToken);
        return Ok(lista.Select(item => Map(item.Medico, item.Especialidades)).ToList());
    }

    [HttpGet("{medicoId:guid}")]
    [ProducesResponseType(typeof(MedicoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicoDto>> GetById(Guid medicoId, CancellationToken cancellationToken)
    {
        var medico = await medicos.ObtenerPorIdAsync(medicoId, cancellationToken);
        if (medico is null)
        {
            return NotFound();
        }

        var especialidades = (await medicos.ListarEspecialidadesActivasAsync(cancellationToken))
            .Where(relacion => relacion.MedicoId == medicoId)
            .ToList();

        return Ok(Map(medico, especialidades));
    }

    [HttpGet("{medicoId:guid}/horarios")]
    [ProducesResponseType(typeof(IReadOnlyList<HorarioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<HorarioDto>>> Horarios(
        Guid medicoId,
        CancellationToken cancellationToken)
    {
        var lista = await horarios.ListarPorMedicoAsync(medicoId, cancellationToken);
        return Ok(lista.Select(horario => new HorarioDto(
            horario.Id,
            horario.MedicoId,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFin)).ToList());
    }

    private static MedicoDto Map(Medico medico, IReadOnlyList<MedicoEspecialidad> especialidades)
    {
        return new MedicoDto(
            medico.Id,
            medico.Nombres,
            medico.Apellidos,
            medico.NombreCompleto,
            medico.NumeroColegiado,
            medico.Telefono,
            especialidades.Select(relacion => relacion.EspecialidadId).ToList(),
            especialidades.FirstOrDefault(relacion => relacion.EsPrimario)?.EspecialidadId);
    }
}
