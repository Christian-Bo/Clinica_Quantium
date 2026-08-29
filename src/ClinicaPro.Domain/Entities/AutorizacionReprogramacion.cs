using ClinicaPro.Domain;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class AutorizacionReprogramacion
{
    public Guid Id { get; private set; }
    public Guid CitaId { get; private set; }
    public Guid SolicitadaPorUsuarioId { get; private set; }
    public Guid? AutorizadaPorUsuarioId { get; private set; }
    public string Estado { get; private set; } = null!;
    public string MotivoSolicitud { get; private set; } = null!;
    public string? MotivoDecision { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DecididaAtUtc { get; private set; }

    private AutorizacionReprogramacion()
    {
    }

    public static AutorizacionReprogramacion Solicitar(Guid citaId, Guid solicitadaPorUsuarioId, string motivo)
    {
        var texto = (motivo ?? string.Empty).Trim();
        if (texto.Length < 5)
        {
            throw new DomainException("El motivo de la solicitud debe tener al menos 5 caracteres.");
        }

        return new AutorizacionReprogramacion
        {
            Id = Guid.NewGuid(),
            CitaId = citaId,
            SolicitadaPorUsuarioId = solicitadaPorUsuarioId,
            Estado = AutorizacionReprogramacionEstados.Pendiente,
            MotivoSolicitud = texto,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Aprobar(Guid administradorUsuarioId, string? motivo)
    {
        ExigirPendiente();
        AutorizadaPorUsuarioId = administradorUsuarioId;
        MotivoDecision = string.IsNullOrWhiteSpace(motivo) ? "Aprobada por el administrador." : motivo.Trim();
        Estado = AutorizacionReprogramacionEstados.Aprobada;
        DecididaAtUtc = DateTime.UtcNow;
    }

    public void Rechazar(Guid administradorUsuarioId, string? motivo)
    {
        ExigirPendiente();
        AutorizadaPorUsuarioId = administradorUsuarioId;
        MotivoDecision = string.IsNullOrWhiteSpace(motivo) ? "Rechazada por el administrador." : motivo.Trim();
        Estado = AutorizacionReprogramacionEstados.Rechazada;
        DecididaAtUtc = DateTime.UtcNow;
    }

    public void MarcarUsada()
    {
        if (Estado != AutorizacionReprogramacionEstados.Aprobada)
        {
            throw new DomainException("Solo una autorización aprobada puede usarse en la reprogramación.");
        }

        Estado = AutorizacionReprogramacionEstados.Usada;
    }

    private void ExigirPendiente()
    {
        if (Estado != AutorizacionReprogramacionEstados.Pendiente)
        {
            throw new DomainException("La autorización ya fue resuelta.");
        }
    }
}
