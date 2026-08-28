namespace ClinicaPro.Domain.Entities;

public sealed class IntentoNotificacion
{
    public long Id { get; private set; }
    public long NotificacionId { get; private set; }
    public DateTime FechaIntentoUtc { get; private set; }
    public bool Exitoso { get; private set; }
    public string? CodigoProveedor { get; private set; }
    public string? RespuestaProveedor { get; private set; }

    private IntentoNotificacion()
    {
    }

    public static IntentoNotificacion Registrar(
        long notificacionId,
        bool exitoso,
        string? codigoProveedor,
        string? respuestaProveedor)
    {
        var respuesta = respuestaProveedor?.Trim();
        if (respuesta is { Length: > 1000 })
        {
            respuesta = respuesta[..1000];
        }

        return new IntentoNotificacion
        {
            NotificacionId = notificacionId,
            FechaIntentoUtc = DateTime.UtcNow,
            Exitoso = exitoso,
            CodigoProveedor = codigoProveedor,
            RespuestaProveedor = respuesta
        };
    }
}
