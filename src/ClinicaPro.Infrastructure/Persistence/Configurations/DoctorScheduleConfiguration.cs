using ClinicaPro.Domain.Doctors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayOfWeek).HasConversion<int>();
        builder.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.IsActive });
    }
}
