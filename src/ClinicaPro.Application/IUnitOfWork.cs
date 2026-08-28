namespace ClinicaPro.Application;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task SaveChangesWithSqlSessionContextAsync(
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default);
}
