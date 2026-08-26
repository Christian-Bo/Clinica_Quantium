using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class NotificacionRepository(ClinicaProDbContext dbContext) : INotificacionRepository
{
    public async Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default)
    {
        await dbContext.Notificaciones.AddAsync(notificacion, cancellationToken);
    }

    public async Task AgregarIntentoAsync(IntentoNotificacion intento, CancellationToken cancellationToken = default)
    {
        await dbContext.IntentosNotificacion.AddAsync(intento, cancellationToken);
    }

    public async Task<IReadOnlyList<Notificacion>> ListarPendientesAsync(
        int cantidadMaxima,
        CancellationToken cancellationToken = default)
    {
        var ahora = DateTime.UtcNow;

        return await dbContext.Notificaciones
            .Where(notificacion =>
                notificacion.Estado == NotificacionEstados.Pendiente
                && (notificacion.ProximoIntentoUtc == null || notificacion.ProximoIntentoUtc <= ahora))
            .OrderBy(notificacion => notificacion.CreatedAtUtc)
            .Take(cantidadMaxima)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notificacion>> ListarPorPacienteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notificaciones.AsNoTracking()
            .Where(notificacion => notificacion.PacienteId == pacienteId)
            .OrderByDescending(notificacion => notificacion.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notificacion>> ListarRecientesAsync(
        int cantidadMaxima,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Notificaciones.AsNoTracking()
            .OrderByDescending(notificacion => notificacion.CreatedAtUtc)
            .Take(cantidadMaxima)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistePorCitaYTipoAsync(
        Guid citaId,
        string tipo,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Notificaciones.AsNoTracking().AnyAsync(
            notificacion => notificacion.CitaId == citaId && notificacion.Tipo == tipo,
            cancellationToken);
    }
}
