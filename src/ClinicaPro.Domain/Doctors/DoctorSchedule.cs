namespace ClinicaPro.Domain.Doctors;

public sealed class DoctorSchedule
{
    private DoctorSchedule() { }

    public DoctorSchedule(Guid id, Guid doctorId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isActive = true)
    {
        if (endTime <= startTime) throw new ArgumentException("La hora final debe ser posterior a la inicial.");

        Id = id;
        DoctorId = doctorId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public Guid DoctorId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsActive { get; private set; }

    public bool Contains(TimeOnly requestedStart, TimeOnly requestedEnd) =>
        IsActive && requestedStart >= StartTime && requestedEnd <= EndTime;
}
