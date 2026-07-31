using ClinicaPro.Domain.Common;
using ClinicaPro.Domain.Doctors;

namespace ClinicaPro.Domain.Appointments;

public static class AppointmentRules
{
    public const int DefaultDurationMinutes = 30;

    public static void ValidateFuture(DateOnly date, TimeOnly startTime, DateTime localNow)
    {
        var requested = date.ToDateTime(startTime);
        if (requested <= localNow)
            throw new BusinessRuleException("La cita debe programarse en una fecha y hora futuras.");
    }

    public static void ValidateSchedule(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        IReadOnlyCollection<DoctorSchedule> schedules)
    {
        var valid = schedules.Any(schedule =>
            schedule.DayOfWeek == date.DayOfWeek &&
            schedule.Contains(startTime, endTime));

        if (!valid)
            throw new BusinessRuleException("El horario solicitado está fuera de la jornada configurada del médico.");
    }
}
