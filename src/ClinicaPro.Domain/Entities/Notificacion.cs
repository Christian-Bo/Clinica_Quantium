namespace ClinicaPro.Domain.Entities;

public sealed class Notificacion
{
    public const int MaxIntentos = 3;

    public long Id { get; private set; }
    public Guid? CitaId { get; private set; }
    public Guid PacienteId { get; private set; }
    public string Canal { get; private set; } = null!;
    public string Tipo { get; private set; } = null!;
    public string Destinatario { get; private set; } = null!;
    public string? Asunto { get; private set; }
    public string Contenido { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public int NumeroIntentos { get; private set; }
    public DateTime? ProximoIntentoUtc { get; private set; }
    public DateTime? EnviadaAtUtc { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notificacion()
    {
    }

    public static Notificacion EncolarEmail(
        Guid pacienteId,
        Guid? citaId,
        string tipo,
        string destinatario,
        string asunto,
        string contenido)
    {
        return new Notificacion
        {
            PacienteId = pacienteId,
            CitaId = citaId,
            Canal = NotificacionCanales.Email,
            Tipo = tipo,
            Destinatario = destinatario.Trim(),
            Asunto = asunto.Trim(),
            Contenido = contenido.Trim(),
            Estado = NotificacionEstados.Pendiente,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void MarcarProcesando()
    {
        Estado = NotificacionEstados.Procesando;
    }

    public void MarcarEnviada()
    {
        Estado = NotificacionEstados.Enviada;
        EnviadaAtUtc = DateTime.UtcNow;
        ProximoIntentoUtc = null;
        UltimoError = null;
        NumeroIntentos++;
    }

    public void MarcarIntentoFallido(string error)
    {
        NumeroIntentos++;
        UltimoError = error.Length > 1000 ? error[..1000] : error;

        if (NumeroIntentos >= MaxIntentos)
        {
            Estado = NotificacionEstados.Fallida;
            ProximoIntentoUtc = null;
            return;
        }

        Estado = NotificacionEstados.Pendiente;
        ProximoIntentoUtc = DateTime.UtcNow.AddMinutes(2 * NumeroIntentos);
    }
}
