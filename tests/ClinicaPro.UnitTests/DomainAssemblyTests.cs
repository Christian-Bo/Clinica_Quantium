using ClinicaPro.Domain;

namespace ClinicaPro.UnitTests;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void DomainAssembly_CanBeResolved()
    {
        Assert.NotNull(DomainAssembly.Reference);
    }
}
