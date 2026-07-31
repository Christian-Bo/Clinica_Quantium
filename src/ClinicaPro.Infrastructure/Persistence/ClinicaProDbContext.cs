using ClinicaPro.Domain.Appointments;
using ClinicaPro.Domain.Catalogs;
using ClinicaPro.Domain.Doctors;
using ClinicaPro.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

public sealed class ClinicaProDbContext : DbContext
{
    public ClinicaProDbContext(DbContextOptions<ClinicaProDbContext> options) : base(options) { }

    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorSpecialty> DoctorSpecialties => Set<DoctorSpecialty>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<Patient> Patients => Set<Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicaProDbContext).Assembly);
    }
}
