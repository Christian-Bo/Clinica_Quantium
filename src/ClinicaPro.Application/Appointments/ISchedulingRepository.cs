using ClinicaPro.Domain.Doctors;

namespace ClinicaPro.Application.Appointments;

public interface ISchedulingRepository
{
    Task<Guid?> GetPrimaryDoctorIdAsync(Guid specialtyId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<DoctorSchedule>> GetActiveSchedulesAsync(Guid doctorId, CancellationToken cancellationToken);
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken);
}
