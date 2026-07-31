using ClinicaPro.Application.Appointments;
using ClinicaPro.Domain.Doctors;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Repositories;

public sealed class SchedulingRepository : ISchedulingRepository
{
    private readonly ClinicaProDbContext _dbContext;

    public SchedulingRepository(ClinicaProDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> GetPrimaryDoctorIdAsync(Guid specialtyId, CancellationToken cancellationToken) =>
        _dbContext.DoctorSpecialties
            .Where(x => x.SpecialtyId == specialtyId && x.IsPrimary && x.IsActive)
            .Join(
                _dbContext.Doctors.Where(x => x.IsActive),
                relationship => relationship.DoctorId,
                doctor => doctor.Id,
                (relationship, doctor) => (Guid?)doctor.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<DoctorSchedule>> GetActiveSchedulesAsync(
        Guid doctorId,
        CancellationToken cancellationToken) =>
        await _dbContext.DoctorSchedules
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .ToListAsync(cancellationToken);

    public Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken) =>
        _dbContext.Patients.AnyAsync(x => x.Id == patientId && x.IsActive, cancellationToken);
}
