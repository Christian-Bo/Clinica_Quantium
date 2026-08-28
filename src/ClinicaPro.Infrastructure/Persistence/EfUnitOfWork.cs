using ClinicaPro.Application;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

public sealed class EfUnitOfWork(ClinicaProDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return EjecutarConReintentosAsync(() => dbContext.SaveChangesAsync(cancellationToken));
    }

    public Task SaveChangesWithSqlSessionContextAsync(
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sys.sp_set_session_context @key=N'UsuarioId', @value={usuarioId.ToString()};",
                    cancellationToken);
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sys.sp_set_session_context @key=N'MotivoCambio', @value={motivo};",
                    cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaccion.CommitAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                await transaccion.RollbackAsync(cancellationToken);
                throw SqlServerExceptionMapper.Map(exception);
            }
        });
    }

    private Task EjecutarConReintentosAsync(Func<Task<int>> accion)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            try
            {
                await accion();
            }
            catch (Exception exception)
            {
                throw SqlServerExceptionMapper.Map(exception);
            }
        });
    }
}
