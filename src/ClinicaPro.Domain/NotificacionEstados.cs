namespace ClinicaPro.Domain;

public static class NotificacionEstados
{
    public const string Pendiente = "Pendiente";
    public const string Procesando = "Procesando";
    public const string Enviada = "Enviada";
    public const string Fallida = "Fallida";
}

public static class NotificacionCanales
{
    public const string Email = "Email";
    public const string WhatsApp = "WhatsApp";
}

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

    public static string? DesdeEstadoCita(string estado) => estado switch
    {
        CitaEstados.Solicitada => SolicitudRecibida,
        CitaEstados.Programada => CitaProgramada,
        CitaEstados.Confirmada => CitaConfirmada,
        CitaEstados.Rechazada => CitaRechazada,
        CitaEstados.Cancelada => CitaCancelada,
        CitaEstados.NoPresentada => CitaNoPresentada,
        _ => null
    };
}
