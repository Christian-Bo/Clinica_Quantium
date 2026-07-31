using ClinicaPro.Domain.Appointments;

namespace ClinicaPro.Application.Appointments;

public interface IAppointmentRepository
{
    Task<bool> HasActiveOverlapAsync(
        Guid doctorId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
}
