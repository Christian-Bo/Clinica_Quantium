using ClinicaPro.Application.Admin;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class RolesStaffAdministrablesTests
{
    [Fact]
    public void NormalizarUno_Secretaria_DevuelveCanonico()
    {
        Assert.Equal(RolNombres.Secretaria, RolesStaffAdministrables.NormalizarUno("secretaria"));
    }

    [Fact]
    public void Normalizar_Medico_LanzaExcepcion()
    {
        var exception = Assert.Throws<DomainException>(
            () => RolesStaffAdministrables.Normalizar(["Medico"]));

        Assert.Contains("/api/admin/medicos", exception.Message);
    }

    [Fact]
    public void Normalizar_RolInventado_LanzaExcepcion()
    {
        var exception = Assert.Throws<DomainException>(
            () => RolesStaffAdministrables.Normalizar(["Inventado"]));

        Assert.Contains("Inventado", exception.Message);
    }
}
