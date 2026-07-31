namespace ClinicaPro.Domain.Appointments;

public sealed class Appointment
{
    private Appointment() { }

    public Guid Id { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public AppointmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Appointment Request(
        Guid patientId,
        Guid doctorId,
        Guid specialtyId,
        DateOnly date,
        TimeOnly startTime,
        int durationMinutes,
        string reason,
        DateTime createdAtUtc)
    {
        if (patientId == Guid.Empty) throw new ArgumentException("El paciente es obligatorio.", nameof(patientId));
        if (doctorId == Guid.Empty) throw new ArgumentException("El médico es obligatorio.", nameof(doctorId));
        if (specialtyId == Guid.Empty) throw new ArgumentException("La especialidad es obligatoria.", nameof(specialtyId));
        if (durationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            throw new ArgumentException("El motivo debe contener al menos 5 caracteres.", nameof(reason));

        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            DoctorId = doctorId,
            SpecialtyId = specialtyId,
            Date = date,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(durationMinutes),
            Reason = reason.Trim(),
            Status = AppointmentStatus.Requested,
            CreatedAtUtc = createdAtUtc
        };
    }
}
