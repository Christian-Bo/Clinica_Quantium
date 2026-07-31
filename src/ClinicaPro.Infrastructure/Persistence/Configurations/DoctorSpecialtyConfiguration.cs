using ClinicaPro.Domain.Doctors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicaPro.Infrastructure.Persistence.Configurations;

public sealed class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.ToTable("DoctorSpecialties");
        builder.HasKey(x => new { x.DoctorId, x.SpecialtyId });
        builder.HasIndex(x => new { x.SpecialtyId, x.IsPrimary, x.IsActive });
    }
}
