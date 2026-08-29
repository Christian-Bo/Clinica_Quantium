using ClinicaPro.Api.Security;
using ClinicaPro.Application;
using ClinicaPro.Application.Admin;
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
    AdministrarParametrosService parametros,
    IAdminStaffService staff,
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
            usuarioId.Value,
            cancellationToken);

        return Created(
            $"/api/medicos/{medicoId}/horarios",
            new HorarioDto(horario.Id, horario.MedicoId, horario.DiaSemana, horario.HoraInicio, horario.HoraFin));
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
}
