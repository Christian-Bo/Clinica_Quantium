namespace ClinicaPro.Domain;

public static class AutorizacionReprogramacionEstados
{
    public const string Pendiente = "Pendiente";
    public const string Aprobada = "Aprobada";
    public const string Rechazada = "Rechazada";
    public const string Usada = "Usada";
}

public static class ParametrosClave
{
    public const string MaximoReprogramaciones = "Citas.MaximoReprogramaciones";
    public const string HorasMinimasCancelacion = "Citas.HorasMinimasCancelacion";
    public const string DuracionPredeterminadaMinutos = "Citas.DuracionPredeterminadaMinutos";
    public const string MaximoActivasPorPaciente = "Citas.MaximoActivasPorPaciente";
}
