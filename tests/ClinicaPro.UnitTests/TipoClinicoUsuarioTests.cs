using ClinicaPro.Application.Admin;
using ClinicaPro.Domain;

namespace ClinicaPro.UnitTests;

public sealed class TipoClinicoUsuarioTests
{
    [Fact]
    public void Inferir_SoloStaff_EsNinguno()
    {
        Assert.Equal(TipoClinicoUsuario.Ninguno, TipoClinicoUsuario.Inferir([RolNombres.Administrador, RolNombres.Secretaria]));
    }

    [Fact]
    public void Inferir_MedicoYPaciente_Lanza()
    {
        var error = Assert.Throws<ClinicaPro.Domain.Exceptions.DomainException>(
            () => TipoClinicoUsuario.Inferir([RolNombres.Medico, RolNombres.Paciente]));

        Assert.Contains("combinarse", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inferir_AdminSecretariaYPaciente_EsPaciente()
    {
        Assert.Equal(
            TipoClinicoUsuario.Paciente,
            TipoClinicoUsuario.Inferir([RolNombres.Administrador, RolNombres.Secretaria, RolNombres.Paciente]));
    }

    [Fact]
    public void Normalizar_ValoresAceptados()
    {
        Assert.Equal(TipoClinicoUsuario.Ninguno, TipoClinicoUsuario.Normalizar(null));
        Assert.Equal(TipoClinicoUsuario.Medico, TipoClinicoUsuario.Normalizar("medico"));
        Assert.Equal(TipoClinicoUsuario.Paciente, TipoClinicoUsuario.Normalizar("Paciente"));
    }
}
