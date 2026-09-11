using ClinicaPro.Api.Security;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicaPro.Api.Controllers;

[ApiController]
[Authorize]
public sealed class ImagenesIrisController(
    SubirImagenIrisService subirImagen,
    ListarImagenesIrisService listarImagenes,
    DescargarImagenIrisService descargarImagen,
    DesactivarImagenIrisService desactivarImagen) : ControllerBase
{
    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpPost("api/citas/{citaId:guid}/imagenes-iris")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImagenIrisDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ImagenIrisDto>> Subir(
        Guid citaId,
        [FromForm] SubirImagenIrisForm form,
        CancellationToken cancellationToken)
    {
        var usuarioId = User.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        if (form.Archivo is null || form.Archivo.Length == 0)
        {
            return BadRequest(new { error = "Debe adjuntar un archivo de imagen." });
        }

        using var memoria = new MemoryStream();
        await form.Archivo.CopyToAsync(memoria, cancellationToken);

        var imagen = await subirImagen.ExecuteAsync(
            citaId,
            usuarioId.Value,
            new SubirImagenIrisInput(
                form.Archivo.FileName,
                form.Archivo.ContentType,
                memoria.ToArray(),
                form.Lateralidad,
                form.Observacion),
            cancellationToken);

        return Created(
            $"/api/imagenes-iris/{imagen.Id}/archivo",
            new ImagenIrisDto(
                imagen.Id,
                imagen.CitaId,
                imagen.TomadaPorUsuarioId,
                imagen.Lateralidad,
                imagen.NombreArchivo,
                imagen.TipoContenido,
                imagen.TamanoBytes,
                imagen.Observacion,
                imagen.FechaCapturaUtc));
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador + "," + RolNombres.Medico)]
    [HttpGet("api/citas/{citaId:guid}/imagenes-iris")]
    [ProducesResponseType(typeof(IReadOnlyList<ImagenIrisDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImagenIrisDto>>> Listar(
        Guid citaId,
        CancellationToken cancellationToken)
    {
        var lista = await listarImagenes.ExecuteAsync(citaId, cancellationToken);
        return Ok(lista.Select(item => new ImagenIrisDto(
            item.ImagenIrisId,
            item.CitaId,
            item.TomadaPorUsuarioId,
            item.Lateralidad,
            item.NombreArchivo,
            item.TipoContenido,
            item.TamanoBytes,
            item.Observacion,
            item.FechaCapturaUtc)).ToList());
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador + "," + RolNombres.Medico)]
    [HttpGet("api/imagenes-iris/{imagenIrisId:guid}/archivo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Descargar(
        Guid imagenIrisId,
        CancellationToken cancellationToken)
    {
        var archivo = await descargarImagen.ExecuteAsync(imagenIrisId, cancellationToken);
        return archivo is null
            ? NotFound()
            : File(archivo.Imagen, archivo.TipoContenido, archivo.NombreArchivo);
    }

    [Authorize(Roles = RolNombres.Secretaria + "," + RolNombres.Administrador)]
    [HttpDelete("api/imagenes-iris/{imagenIrisId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Desactivar(
        Guid imagenIrisId,
        CancellationToken cancellationToken)
    {
        await desactivarImagen.ExecuteAsync(imagenIrisId, cancellationToken);
        return NoContent();
    }
}