namespace ClinicaPro.Contracts.Appointments;

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid SpecialtyId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    string Reason,
    DateTime CreatedAtUtc);
