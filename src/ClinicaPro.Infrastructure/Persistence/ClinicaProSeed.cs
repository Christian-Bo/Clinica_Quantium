using ClinicaPro.Domain.Catalogs;
using ClinicaPro.Domain.Doctors;
using ClinicaPro.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence;

public static class ClinicaProSeed
{
    public static readonly Guid DemoPatientId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid NaturalMedicineSpecialtyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid NutritionSpecialtyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid PrimaryDoctorId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public static async Task SeedAsync(ClinicaProDbContext dbContext)
    {
        if (await dbContext.Specialties.AnyAsync()) return;

        dbContext.Specialties.AddRange(
            new Specialty(NaturalMedicineSpecialtyId, "Medicina natural"),
            new Specialty(NutritionSpecialtyId, "Nutrición"));

        dbContext.Doctors.Add(new Doctor(PrimaryDoctorId, "Médico principal de demostración"));
        dbContext.Patients.Add(new Patient(DemoPatientId, "Paciente de demostración", "5555-5555"));

        dbContext.DoctorSpecialties.AddRange(
            new DoctorSpecialty(PrimaryDoctorId, NaturalMedicineSpecialtyId, true),
            new DoctorSpecialty(PrimaryDoctorId, NutritionSpecialtyId, true));

        var schedules = new List<DoctorSchedule>();
        foreach (var day in new[]
                 {
                     DayOfWeek.Monday,
                     DayOfWeek.Tuesday,
                     DayOfWeek.Wednesday,
                     DayOfWeek.Thursday,
                     DayOfWeek.Friday
                 })
        {
            schedules.Add(new DoctorSchedule(
                Guid.NewGuid(),
                PrimaryDoctorId,
                day,
                new TimeOnly(6, 0),
                new TimeOnly(13, 0)));
        }

        schedules.Add(new DoctorSchedule(
            Guid.NewGuid(),
            PrimaryDoctorId,
            DayOfWeek.Saturday,
            new TimeOnly(7, 0),
            new TimeOnly(12, 0)));

        dbContext.DoctorSchedules.AddRange(schedules);
        await dbContext.SaveChangesAsync();
    }
}
