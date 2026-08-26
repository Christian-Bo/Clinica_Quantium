using ClinicaPro.Application;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class AuditoriaWriter(ClinicaProDbContext dbContext) : IAuditoriaWriter
{
    public async Task RegistrarAsync(
        Guid? usuarioId,
        string accion,
        string entidad,
        string? entidadId,
        string? detalle,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Auditoria.AddAsync(
            RegistroAuditoria.Crear(usuarioId, accion, entidad, entidadId, detalle),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RegistroAuditoria>> ListarRecientesAsync(
        int cantidadMaxima,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Auditoria.AsNoTracking()
            .OrderByDescending(item => item.FechaUtc)
            .Take(cantidadMaxima)
            .ToListAsync(cancellationToken);
    }
}
