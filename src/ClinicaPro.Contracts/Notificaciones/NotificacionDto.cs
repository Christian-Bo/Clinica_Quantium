namespace ClinicaPro.Contracts.Notificaciones;

public sealed record NotificacionDto(
    long NotificacionId,
    Guid? CitaId,
    Guid PacienteId,
    string Canal,
    string Tipo,
    string Destinatario,
    string? Asunto,
    string Contenido,
    string Estado,
    int NumeroIntentos,
    DateTime? EnviadaAtUtc,
    DateTime CreatedAtUtc);
