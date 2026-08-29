namespace ClinicaPro.Client.Shared.Constants;

/// <summary>Debe coincidir con ClinicaPro.Domain.NotificacionEstados (backend).</summary>
public static class NotificacionEstados
{
    public const string Pendiente = "Pendiente";
    public const string Procesando = "Procesando";
    public const string Enviada = "Enviada";
    public const string Fallida = "Fallida";
}

/// <summary>Debe coincidir con ClinicaPro.Domain.NotificacionTipos (backend).</summary>
public static class NotificacionTipos
{
    public const string SolicitudRecibida = "SolicitudRecibida";
    public const string CitaProgramada = "CitaProgramada";
    public const string CitaConfirmada = "CitaConfirmada";
    public const string CitaRechazada = "CitaRechazada";
    public const string CitaCancelada = "CitaCancelada";
    public const string CitaReprogramada = "CitaReprogramada";
    public const string CitaNoPresentada = "CitaNoPresentada";
    public const string RecordatorioCita = "RecordatorioCita";

    public static string Etiqueta(string tipo) => tipo switch
    {
        SolicitudRecibida => "Solicitud recibida",
        CitaProgramada => "Cita programada",
        CitaConfirmada => "Cita confirmada",
        CitaRechazada => "Cita rechazada",
        CitaCancelada => "Cita cancelada",
        CitaReprogramada => "Cita reprogramada",
        CitaNoPresentada => "No se presentó",
        RecordatorioCita => "Recordatorio",
        _ => tipo
    };
}
