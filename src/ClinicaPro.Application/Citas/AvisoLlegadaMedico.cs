namespace ClinicaPro.Application.Citas;

public sealed record PacienteLlegoAviso(
    Guid CitaId,
    Guid PacienteId,
    string PacienteNombre,
    DateTime FechaHoraInicio);

public interface IAvisoTiempoRealAgenda
{
    Task PacienteLlegoAsync(Guid medicoUsuarioId, PacienteLlegoAviso aviso, CancellationToken cancellationToken = default);
}

public sealed class AvisoTiempoRealAgendaNulo : IAvisoTiempoRealAgenda
{
    public Task PacienteLlegoAsync(
        Guid medicoUsuarioId,
        PacienteLlegoAviso aviso,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
