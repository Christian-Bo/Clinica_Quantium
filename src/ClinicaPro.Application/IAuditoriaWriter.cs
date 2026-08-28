using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application;

public interface IAuditoriaWriter
{
    Task RegistrarAsync(
        Guid? usuarioId,
        string accion,
        string entidad,
        string? entidadId,
        string? detalle,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RegistroAuditoria>> ListarRecientesAsync(
        int cantidadMaxima,
        CancellationToken cancellationToken = default);
}
