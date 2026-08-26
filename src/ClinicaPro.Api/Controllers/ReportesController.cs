using ClinicaPro.Application.Pacientes;
using ClinicaPro.Contracts.Reportes;
using ClinicaPro.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
[Route("api/reportes")]
public sealed class ReportesController(ListarReporteCitasService listarReporte) : ControllerBase
{
    [HttpGet("citas")]
    [ProducesResponseType(typeof(ReporteCitasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReporteCitasDto>> Citas(
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] Guid? medicoId,
        CancellationToken cancellationToken)
    {
        var reporte = await listarReporte.ExecuteAsync(desde, hasta, medicoId, cancellationToken);
        return Ok(new ReporteCitasDto(
            reporte.Desde,
            reporte.Hasta,
            reporte.Total,
            reporte.PorEstado.Select(item => new ConteoEstadoDto(item.Estado, item.Cantidad)).ToList(),
            reporte.Atendidas,
            reporte.Canceladas,
            reporte.NoPresentadas,
            reporte.Reprogramadas));
    }
}
