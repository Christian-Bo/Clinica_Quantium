using ClinicaPro.Api.Security;
using ClinicaPro.Application;
using ClinicaPro.Application.Admin;
using ClinicaPro.Application.Citas;
using ClinicaPro.Contracts.Admin;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Contracts.Especialidades;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolNombres.Administrador)]
[Route("api/admin")]
public sealed class AdminController(
    AdministrarEspecialidadesService especialidades,
    AdministrarHorariosService horarios,
    AdministrarMedicoEspecialidadesService medicoEspecialidades,
    AdministrarParametrosService parametros,
    IAdminStaffService staff,
    ListarAutorizacionesReprogramacionService listarAutorizaciones,
    ResolverAutorizacionReprogramacionService resolverAutorizaciones,
    IAuditoriaWriter auditoria) : ControllerBase
{
    [HttpGet("especialidades")]
    public async Task<ActionResult<IReadOnlyList<EspecialidadDto>>> Especialidades(CancellationToken cancellationToken)
    {
        var lista = await especialidades.ListarAsync(cancellationToken);
        return Ok(lista.Select(item => new EspecialidadDto(item.Id, item.Nombre, item.Descripcion)).ToList());
    }

    [HttpPost("especialidades")]
    public async Task<ActionResult<EspecialidadDto>> CrearEspecialidad(
        [FromBody] CrearEspecialidadRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var creada = await especialidades.CrearAsync(request.Nombre, request.Descripcion, usuarioId.Value, cancellationToken);
        return Created($"/api/admin/especialidades/{creada.Id}", new EspecialidadDto(creada.Id, creada.Nombre, creada.Descripcion));
    }

    [HttpPut("especialidades/{especialidadId:guid}")]
    public async Task<ActionResult<EspecialidadDto>> ActualizarEspecialidad(
        Guid especialidadId,
        [FromBody] ActualizarEspecialidadRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var actualizada = await especialidades.ActualizarAsync(
            especialidadId,
            request.Nombre,
            request.Descripcion,
            request.IsActive,
            usuarioId.Value,
            cancellationToken);
        return Ok(new EspecialidadDto(actualizada.Id, actualizada.Nombre, actualizada.Descripcion));
    }

    [HttpGet("medicos")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminMedicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminMedicoDto>>> Medicos(CancellationToken cancellationToken)
    {
        var lista = await staff.ListarMedicosAsync(cancellationToken);
        return Ok(lista.Select(item => new AdminMedicoDto(
            item.MedicoId,
            item.UsuarioId,
            item.Email,
            item.Nombres,
            item.Apellidos,
            item.NombreCompleto,
            item.NumeroColegiado,
            item.Telefono,
            item.IsActive,
            item.EspecialidadIds,
            item.EspecialidadPrimariaId)).ToList());
    }

    [HttpPost("medicos")]
    public async Task<ActionResult<MedicoDto>> CrearMedico(
        [FromBody] CrearMedicoRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var medico = await staff.CrearMedicoAsync(
            new CrearMedicoInput(
                request.Email,
                request.Password,
                request.Nombres,
                request.Apellidos,
                request.NumeroColegiado,
                request.Telefono,
                request.EspecialidadId,
                request.EsPrimario),
            usuarioId.Value,
            cancellationToken);

        return Created($"/api/medicos/{medico.Id}", MapMedico(medico, [request.EspecialidadId], request.EsPrimario ? request.EspecialidadId : null));
    }

    [HttpGet("medicos/{medicoId:guid}/especialidades")]
    public async Task<ActionResult<IReadOnlyList<MedicoEspecialidadAdminDto>>> EspecialidadesDeMedico(
        Guid medicoId,
        CancellationToken cancellationToken)
    {
        var lista = await medicoEspecialidades.ListarAsync(medicoId, cancellationToken);
        return Ok(lista.Select(MapEspecialidadMedico).ToList());
    }

    [HttpPost("medicos/{medicoId:guid}/especialidades")]
    public async Task<ActionResult<MedicoEspecialidadAdminDto>> AgregarEspecialidadMedico(
        Guid medicoId,
        [FromBody] AsignarEspecialidadMedicoRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var creada = await medicoEspecialidades.AgregarAsync(
            medicoId,
            request.EspecialidadId,
            request.EsPrimario,
            usuarioId.Value,
            cancellationToken);

        return Created(
            $"/api/admin/medicos/{medicoId}/especialidades/{creada.EspecialidadId}",
            MapEspecialidadMedico(creada));
    }

    [HttpPut("medicos/{medicoId:guid}/especialidades/{especialidadId:guid}")]
    public async Task<ActionResult<MedicoEspecialidadAdminDto>> ActualizarEspecialidadMedico(
        Guid medicoId,
        Guid especialidadId,
        [FromBody] ActualizarEspecialidadMedicoRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var actualizada = await medicoEspecialidades.ActualizarAsync(
            medicoId,
            especialidadId,
            request.EsPrimario,
            request.IsActive,
            usuarioId.Value,
            cancellationToken);
        return Ok(MapEspecialidadMedico(actualizada));
    }

    [HttpDelete("medicos/{medicoId:guid}/especialidades/{especialidadId:guid}")]
    public async Task<IActionResult> QuitarEspecialidadMedico(
        Guid medicoId,
        Guid especialidadId,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        await medicoEspecialidades.QuitarAsync(medicoId, especialidadId, usuarioId.Value, cancellationToken);
        return NoContent();
    }

    [HttpPut("medicos/{medicoId:guid}")]
    public async Task<ActionResult<MedicoDto>> ActualizarMedico(
        Guid medicoId,
        [FromBody] ActualizarMedicoRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var medico = await staff.ActualizarMedicoAsync(
            medicoId,
            request.Nombres,
            request.Apellidos,
            request.NumeroColegiado,
            request.Telefono,
            request.IsActive,
            usuarioId.Value,
            cancellationToken);
        return Ok(MapMedico(medico, [], null));
    }

    [HttpPost("medicos/{medicoId:guid}/horarios")]
    public async Task<ActionResult<HorarioDto>> CrearHorario(
        Guid medicoId,
        [FromBody] CrearHorarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var horario = await horarios.CrearAsync(
            medicoId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.VigenteDesde,
            request.VigenteHasta,
            usuarioId.Value,
            cancellationToken);

        return Created(
            $"/api/admin/medicos/{medicoId}/horarios/{horario.Id}",
            MapHorario(horario));
    }

    [HttpPut("medicos/{medicoId:guid}/horarios/{horarioId:guid}")]
    public async Task<ActionResult<HorarioDto>> ActualizarHorario(
        Guid medicoId,
        Guid horarioId,
        [FromBody] ActualizarHorarioRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var horario = await horarios.ActualizarAsync(
            medicoId,
            horarioId,
            request.DiaSemana,
            request.HoraInicio,
            request.HoraFin,
            request.VigenteDesde,
            request.VigenteHasta,
            request.IsActive,
            usuarioId.Value,
            cancellationToken);

        return Ok(MapHorario(horario));
    }

    [HttpDelete("horarios/{horarioId:guid}")]
    public async Task<IActionResult> EliminarHorario(Guid horarioId, CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        await horarios.EliminarAsync(horarioId, usuarioId.Value, cancellationToken);
        return NoContent();
    }

    [HttpGet("usuarios")]
    public async Task<ActionResult<IReadOnlyList<UsuarioAdminDto>>> Usuarios(CancellationToken cancellationToken)
    {
        var lista = await staff.ListarUsuariosAsync(cancellationToken);
        return Ok(lista.Select(item => new UsuarioAdminDto(item.UsuarioId, item.Email, item.IsActive, item.Roles)).ToList());
    }

    [HttpPut("usuarios/{usuarioId:guid}")]
    public async Task<IActionResult> ActualizarUsuario(
        Guid usuarioId,
        [FromBody] ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.ObtenerUsuarioId();
        if (actorId is null)
        {
            return Unauthorized();
        }

        await staff.CambiarActivoUsuarioAsync(usuarioId, request.IsActive, actorId.Value, cancellationToken);
        return Ok();
    }

    [HttpPost("usuarios")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAdminDto>> CrearUsuario(
        [FromBody] CrearUsuarioStaffRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.ObtenerUsuarioId();
        if (actorId is null)
        {
            return Unauthorized();
        }

        var creado = await staff.CrearUsuarioStaffAsync(
            request.Email,
            request.Password,
            request.Rol,
            actorId.Value,
            cancellationToken);

        return Created(
            $"/api/admin/usuarios/{creado.UsuarioId}",
            new UsuarioAdminDto(creado.UsuarioId, creado.Email, creado.IsActive, creado.Roles));
    }

    [HttpPut("usuarios/{usuarioId:guid}/roles")]
    public async Task<ActionResult<UsuarioAdminDto>> ActualizarRoles(
        Guid usuarioId,
        [FromBody] ActualizarRolesUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.ObtenerUsuarioId();
        if (actorId is null)
        {
            return Unauthorized();
        }

        var actualizado = await staff.ActualizarRolesAsync(
            usuarioId,
            request.Roles ?? [],
            actorId.Value,
            cancellationToken);

        return Ok(new UsuarioAdminDto(actualizado.UsuarioId, actualizado.Email, actualizado.IsActive, actualizado.Roles));
    }

    [HttpGet("autorizaciones-reprogramacion")]
    public async Task<ActionResult<IReadOnlyList<AutorizacionReprogramacionDto>>> Autorizaciones(
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        var lista = await listarAutorizaciones.ExecuteAsync(estado, cancellationToken);
        return Ok(lista.Select(MapAutorizacion).ToList());
    }

    [HttpPost("autorizaciones-reprogramacion/{autorizacionId:guid}/aprobar")]
    public async Task<ActionResult<AutorizacionReprogramacionDto>> AprobarAutorizacion(
        Guid autorizacionId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
    {
        var actorId = User.ObtenerUsuarioId();
        if (actorId is null)
        {
            return Unauthorized();
        }

        var autorizacion = await resolverAutorizaciones.AprobarAsync(
            autorizacionId,
            actorId.Value,
            request?.Motivo,
            cancellationToken);
        return Ok(MapAutorizacion(autorizacion));
    }

    [HttpPost("autorizaciones-reprogramacion/{autorizacionId:guid}/rechazar")]
    public async Task<ActionResult<AutorizacionReprogramacionDto>> RechazarAutorizacion(
        Guid autorizacionId,
        [FromBody] MotivoCitaRequest? request,
        CancellationToken cancellationToken)
    {
        var actorId = User.ObtenerUsuarioId();
        if (actorId is null)
        {
            return Unauthorized();
        }

        var autorizacion = await resolverAutorizaciones.RechazarAsync(
            autorizacionId,
            actorId.Value,
            request?.Motivo,
            cancellationToken);
        return Ok(MapAutorizacion(autorizacion));
    }

    [HttpPut("parametros/{clave}")]
    public async Task<IActionResult> ActualizarParametro(
        string clave,
        [FromBody] ActualizarParametroRequest request,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        await parametros.ActualizarAsync(clave, request.Valor, usuarioId.Value, cancellationToken);
        return Ok();
    }

    [HttpGet("auditoria")]
    public async Task<ActionResult<IReadOnlyList<AuditoriaDto>>> Auditoria(CancellationToken cancellationToken)
    {
        var lista = await auditoria.ListarRecientesAsync(100, cancellationToken);
        return Ok(lista.Select(item => new AuditoriaDto(
            item.Id,
            item.UsuarioId,
            item.Accion,
            item.Entidad,
            item.EntidadId,
            item.Detalle,
            item.FechaUtc)).ToList());
    }

    private static MedicoDto MapMedico(Medico medico, IReadOnlyList<Guid> especialidadIds, Guid? primaria)
        => new(
            medico.Id,
            medico.Nombres,
            medico.Apellidos,
            medico.NombreCompleto,
            medico.NumeroColegiado,
            medico.Telefono,
            especialidadIds,
            primaria);

    private static HorarioDto MapHorario(Horario horario)
        => new(
            horario.Id,
            horario.MedicoId,
            horario.DiaSemana,
            horario.HoraInicio,
            horario.HoraFin,
            horario.VigenteDesde,
            horario.VigenteHasta,
            horario.IsActive);

    private static MedicoEspecialidadAdminDto MapEspecialidadMedico(MedicoEspecialidadDetalle item)
        => new(item.EspecialidadId, item.Nombre, item.EsPrimario, item.IsActive);

    private static AutorizacionReprogramacionDto MapAutorizacion(AutorizacionReprogramacion autorizacion)
        => new(
            autorizacion.Id,
            autorizacion.CitaId,
            autorizacion.SolicitadaPorUsuarioId,
            autorizacion.AutorizadaPorUsuarioId,
            autorizacion.Estado,
            autorizacion.MotivoSolicitud,
            autorizacion.MotivoDecision,
            autorizacion.CreatedAtUtc,
            autorizacion.DecididaAtUtc);
}
