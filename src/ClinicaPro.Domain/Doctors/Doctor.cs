namespace ClinicaPro.Domain.Doctors;

public sealed class Doctor
{
    private Doctor() { }

    public Doctor(Guid id, string fullName, bool isActive = true)
    {
        Id = id;
        FullName = fullName;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
