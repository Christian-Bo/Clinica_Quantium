using ClinicaPro.Domain.Appointments;
using ClinicaPro.Domain.Common;
using ClinicaPro.Domain.Doctors;

namespace ClinicaPro.UnitTests.Appointments;

public sealed class AppointmentRulesTests
{
    [Fact]
    public void ValidateFuture_RejectsPastDate()
    {
        var now = new DateTime(2026, 7, 31, 10, 0, 0);

        Assert.Throws<BusinessRuleException>(() =>
            AppointmentRules.ValidateFuture(
                new DateOnly(2026, 7, 31),
                new TimeOnly(9, 30),
                now));
    }

    [Fact]
    public void ValidateSchedule_AcceptsTimeInsideConfiguredSchedule()
    {
        var date = new DateOnly(2026, 8, 3); // Monday
        var schedules = new[]
        {
            new DoctorSchedule(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DayOfWeek.Monday,
                new TimeOnly(6, 0),
                new TimeOnly(13, 0))
        };

        var exception = Record.Exception(() =>
            AppointmentRules.ValidateSchedule(
                date,
                new TimeOnly(8, 0),
                new TimeOnly(8, 30),
                schedules));

        Assert.Null(exception);
    }
}
