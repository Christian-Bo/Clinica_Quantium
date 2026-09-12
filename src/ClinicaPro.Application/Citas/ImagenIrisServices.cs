using System.Security.Cryptography;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed record SubirImagenIrisInput(
    string NombreArchivo,
    string TipoContenido,
    byte[] Contenido,
    string? Lateralidad,
    string? Observacion);

public sealed class SubirImagenIrisService(
    ICitaRepository citas,
    IImagenIrisRepository imagenes,
    IUnitOfWork unitOfWork)
{
    public async Task<ImagenIris> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        SubirImagenIrisInput input,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (cita.Estado is CitaEstados.Cancelada or CitaEstados.Rechazada or CitaEstados.NoPresentada)
        {
            throw new DomainException(
                $"No se pueden adjuntar imágenes a una cita {cita.Estado}.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(input.Contenido)).ToLowerInvariant();

        var imagen = ImagenIris.Registrar(
            citaId,
            usuarioId,
            input.NombreArchivo,
            input.TipoContenido,
            input.Contenido,
            input.Lateralidad,
            hash,
            input.Observacion);

        await imagenes.AgregarAsync(imagen, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return imagen;
    }
}

public sealed class ListarImagenesIrisService(IImagenIrisRepository imagenes)
{
    public Task<IReadOnlyList<ImagenIrisMetadato>> ExecuteAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
        => imagenes.ListarPorCitaAsync(citaId, cancellationToken);
}

public sealed class DescargarImagenIrisService(IImagenIrisRepository imagenes)
{
    public Task<ImagenIrisArchivo?> ExecuteAsync(
        Guid imagenIrisId,
        CancellationToken cancellationToken = default)
        => imagenes.ObtenerArchivoAsync(imagenIrisId, cancellationToken);
}

public sealed class DesactivarImagenIrisService(
    IImagenIrisRepository imagenes,
    IUnitOfWork unitOfWork)
{
    public async Task ExecuteAsync(Guid imagenIrisId, CancellationToken cancellationToken = default)
    {
        var imagen = await imagenes.ObtenerRastreadaAsync(imagenIrisId, cancellationToken)
            ?? throw new DomainException("La imagen no existe.");

        imagen.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}