namespace ClinicaPro.Domain.Patients;

public sealed class Patient
{
    private Patient() { }

    public Patient(Guid id, string fullName, string phone, bool isActive = true)
    {
        Id = id;
        FullName = fullName;
        Phone = phone;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
