using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Notificaciones;

public interface INotificacionRepository
{
    Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default);

    Task AgregarIntentoAsync(IntentoNotificacion intento, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notificacion>> ListarPendientesAsync(
        int cantidadMaxima,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notificacion>> ListarPorPacienteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notificacion>> ListarStaffAsync(
        string? estado,
        DateTime? desdeUtc,
        DateTime? hastaUtcExclusivo,
        int cantidadMaxima,
        CancellationToken cancellationToken = default);

    Task<bool> ExistePorCitaYTipoAsync(
        Guid citaId,
        string tipo,
        CancellationToken cancellationToken = default);

    Task AnularPendientesDeTipoAsync(
        Guid citaId,
        string tipo,
        string motivo,
        CancellationToken cancellationToken = default);
}

public sealed record EmailSendResult(bool Succeeded, string? ProviderCode, string? Response);

public interface IEmailSender
{
    Task<EmailSendResult> SendAsync(
        string destinatario,
        string asunto,
        string contenido,
        CancellationToken cancellationToken = default);
}
