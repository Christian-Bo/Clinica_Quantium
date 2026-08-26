using ClinicaPro.Api.Security;
using ClinicaPro.Application.Notificaciones;
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
    ListarNotificacionesStaffService listarStaff) : ControllerBase
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
        return Ok(lista.Select(Map).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Get(CancellationToken cancellationToken)
    {
        var lista = await listarStaff.ExecuteAsync(cancellationToken);
        return Ok(lista.Select(Map).ToList());
    }

    private static NotificacionDto Map(Notificacion notificacion) => new(
        notificacion.Id,
        notificacion.CitaId,
        notificacion.PacienteId,
        notificacion.Canal,
        notificacion.Tipo,
        notificacion.Destinatario,
        notificacion.Asunto,
        notificacion.Contenido,
        notificacion.Estado,
        notificacion.NumeroIntentos,
        notificacion.EnviadaAtUtc,
        notificacion.CreatedAtUtc);
}
