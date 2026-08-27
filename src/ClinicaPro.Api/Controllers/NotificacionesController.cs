using ClinicaPro.Api.Security;
using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Contracts.Notificaciones;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notificaciones")]
public sealed class NotificacionesController(
    ListarNotificacionesPacienteService listarMias,
    ListarNotificacionesStaffService listarStaff,
    IPacienteRepository pacientes) : ControllerBase
{
    [Authorize(Roles = RolNombres.Paciente)]
    [HttpGet("mias")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Mias(CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var lista = await listarMias.ExecuteAsync(usuarioId.Value, cancellationToken);
        return Ok(await MapManyAsync(lista, cancellationToken));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Get(CancellationToken cancellationToken)
    {
        var lista = await listarStaff.ExecuteAsync(cancellationToken);
        return Ok(await MapManyAsync(lista, cancellationToken));
    }

    private async Task<List<NotificacionDto>> MapManyAsync(
        IReadOnlyList<Notificacion> lista,
        CancellationToken cancellationToken)
    {
        var ids = lista.Select(item => item.PacienteId).Distinct().ToList();
        var nombres = (await pacientes.ListarPorIdsAsync(ids, cancellationToken))
            .ToDictionary(item => item.Id, item => item.NombreCompleto);

        return lista.Select(notificacion => new NotificacionDto(
            notificacion.Id,
            notificacion.CitaId,
            notificacion.PacienteId,
            nombres.GetValueOrDefault(notificacion.PacienteId, string.Empty),
            notificacion.Canal,
            notificacion.Tipo,
            notificacion.Destinatario,
            notificacion.Asunto,
            notificacion.Contenido,
            notificacion.Estado,
            notificacion.NumeroIntentos,
            notificacion.EnviadaAtUtc,
            notificacion.CreatedAtUtc)).ToList();
    }
}
