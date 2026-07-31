namespace ClinicaPro.Domain.Catalogs;

public sealed class Specialty
{
    private Specialty() { }

    public Specialty(Guid id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
