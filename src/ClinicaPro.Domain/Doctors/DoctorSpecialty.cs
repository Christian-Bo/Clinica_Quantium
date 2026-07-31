namespace ClinicaPro.Domain.Doctors;

public sealed class DoctorSpecialty
{
    private DoctorSpecialty() { }

    public DoctorSpecialty(Guid doctorId, Guid specialtyId, bool isPrimary, bool isActive = true)
    {
        DoctorId = doctorId;
        SpecialtyId = specialtyId;
        IsPrimary = isPrimary;
        IsActive = isActive;
    }

    public Guid DoctorId { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
}
