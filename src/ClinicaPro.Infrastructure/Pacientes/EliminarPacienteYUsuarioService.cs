using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Exceptions;
using ClinicaPro.Infrastructure.Identity;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Pacientes;

public sealed class EliminarPacienteYUsuarioService(
    ClinicaProDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IEliminarPacienteYUsuario
{
    public async Task ExecuteAsync(
        Guid pacienteId,
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var citaIds = await dbContext.Citas
                    .Where(cita => cita.PacienteId == pacienteId)
                    .Select(cita => cita.Id)
                    .ToListAsync(cancellationToken);

                var notificacionIds = await dbContext.Notificaciones
                    .Where(item => item.PacienteId == pacienteId)
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken);

                if (notificacionIds.Count > 0)
                {
                    await dbContext.IntentosNotificacion
                        .Where(intento => notificacionIds.Contains(intento.NotificacionId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await dbContext.Notificaciones
                        .Where(item => item.PacienteId == pacienteId)
                        .ExecuteDeleteAsync(cancellationToken);
                }

                if (citaIds.Count > 0)
                {
                    await dbContext.HistorialCitas
                        .Where(item => citaIds.Contains(item.CitaId))
                        .ExecuteDeleteAsync(cancellationToken);
                    await dbContext.Citas
                        .Where(cita => cita.PacienteId == pacienteId)
                        .ExecuteDeleteAsync(cancellationToken);
                }

                await dbContext.Pacientes
                    .Where(paciente => paciente.Id == pacienteId)
                    .ExecuteDeleteAsync(cancellationToken);

                await dbContext.Auditoria
                    .Where(item => item.UsuarioId == usuarioId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(item => item.UsuarioId, (Guid?)null),
                        cancellationToken);

                var user = await userManager.FindByIdAsync(usuarioId.ToString());
                if (user is not null)
                {
                    var borrado = await userManager.DeleteAsync(user);
                    if (!borrado.Succeeded)
                    {
                        throw new DomainException(
                            borrado.Errors.FirstOrDefault()?.Description
                            ?? "No fue posible eliminar la cuenta del paciente.");
                    }
                }

                await transaccion.CommitAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                await transaccion.RollbackAsync(cancellationToken);
                if (exception is DomainException)
                {
                    throw;
                }

                throw SqlServerExceptionMapper.Map(exception);
            }
        });
    }
}
