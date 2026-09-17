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

        if (!CitaEstados.PermiteCapturaClinica(cita.Estado))
        {
            throw new DomainException(
                "Solo se pueden adjuntar imágenes de iris cuando la cita está En Espera o En Atencion.");
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

public sealed class ListarImagenesIrisService(
    ICitaRepository citas,
    IImagenIrisRepository imagenes,
    AccesoExpedienteService accesoExpediente)
{
    public async Task<IReadOnlyList<ImagenIrisMetadato>> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        bool esStaffMostrador,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        await accesoExpediente.ExigirLecturaAsync(
            usuarioId,
            esStaffMostrador,
            cita.PacienteId,
            cancellationToken);

        return await imagenes.ListarPorCitaAsync(citaId, cancellationToken);
    }
}

public sealed class DescargarImagenIrisService(
    IImagenIrisRepository imagenes,
    ICitaRepository citas,
    AccesoExpedienteService accesoExpediente)
{
    public async Task<ImagenIrisArchivo?> ExecuteAsync(
        Guid imagenIrisId,
        Guid usuarioId,
        bool esStaffMostrador,
        CancellationToken cancellationToken = default)
    {
        var archivo = await imagenes.ObtenerArchivoAsync(imagenIrisId, cancellationToken);
        if (archivo is null)
        {
            return null;
        }

        var cita = await citas.ObtenerPorIdAsync(archivo.CitaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        await accesoExpediente.ExigirLecturaAsync(
            usuarioId,
            esStaffMostrador,
            cita.PacienteId,
            cancellationToken);

        return archivo;
    }
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