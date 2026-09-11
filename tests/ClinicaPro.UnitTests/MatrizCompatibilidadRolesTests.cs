using ClinicaPro.Application.Admin;
using ClinicaPro.Domain;

namespace ClinicaPro.UnitTests;

public sealed class MatrizCompatibilidadRolesTests
{
    [Fact]
    public void ExigirCompatible_AdminSecretariaYPaciente_NoLanza()
    {
        MatrizCompatibilidadRoles.ExigirCompatible(
            [RolNombres.Administrador, RolNombres.Secretaria],
            TipoClinicoUsuario.Paciente);
    }

    [Fact]
    public void ExigirCompatible_AdminYMedico_NoLanza()
    {
        MatrizCompatibilidadRoles.ExigirCompatible(
            [RolNombres.Administrador],
            TipoClinicoUsuario.Medico);
    }

    [Fact]
    public void ExigirCompatible_SoloStaff_NoLanza()
    {
        MatrizCompatibilidadRoles.ExigirCompatible(
            [RolNombres.Secretaria],
            TipoClinicoUsuario.Ninguno);
    }
}
