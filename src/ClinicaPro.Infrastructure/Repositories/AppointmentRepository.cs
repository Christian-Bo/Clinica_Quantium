using ClinicaPro.Application.Appointments;
using ClinicaPro.Domain.Appointments;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private readonly ClinicaProDbContext _dbContext;

    public AppointmentRepository(ClinicaProDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasActiveOverlapAsync(
        Guid doctorId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        CancellationToken cancellationToken)
    {
        var inactiveStatuses = new[]
        {
            AppointmentStatus.Cancelled,
            AppointmentStatus.Rejected,
            AppointmentStatus.NoShow,
            AppointmentStatus.Attended
        };

        return _dbContext.Appointments.AnyAsync(
            appointment =>
                appointment.DoctorId == doctorId &&
                appointment.Date == date &&
                !inactiveStatuses.Contains(appointment.Status) &&
                appointment.StartTime < endTime &&
                startTime < appointment.EndTime,
            cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        _dbContext.Appointments.Add(appointment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
