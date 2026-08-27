namespace ClinicaPro.Domain;

public static class HoraClinica
{
    public static DateTime Ahora()
    {
        var zona = ZonaGuatemala();
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona);
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }

    public static DateTime ALocal(DateTime utc)
    {
        var instanteUtc = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        var local = TimeZoneInfo.ConvertTimeFromUtc(instanteUtc, ZonaGuatemala());
        return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
    }

    public static DateTime AUtc(DateTime valor)
    {
        if (valor.Kind == DateTimeKind.Utc)
        {
            return valor;
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(valor, DateTimeKind.Unspecified),
            ZonaGuatemala());
    }

    public static byte DiaSemana(DateTime fecha)
    {
        return fecha.DayOfWeek == DayOfWeek.Sunday
            ? (byte)7
            : (byte)fecha.DayOfWeek;
    }

    private static TimeZoneInfo ZonaGuatemala()
    {
        foreach (var id in new[] { "Central America Standard Time", "America/Guatemala" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Guatemala",
            TimeSpan.FromHours(-6),
            "Guatemala",
            "Guatemala");
    }
}
