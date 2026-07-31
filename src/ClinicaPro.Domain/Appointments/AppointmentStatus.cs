namespace ClinicaPro.Domain.Appointments;

public enum AppointmentStatus
{
    Requested = 1,
    Scheduled = 2,
    Confirmed = 3,
    Waiting = 4,
    InProgress = 5,
    Attended = 6,
    Cancelled = 7,
    NoShow = 8,
    Rejected = 9
}
