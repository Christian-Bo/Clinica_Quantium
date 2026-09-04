using ClinicaPro.Application.Auth;

namespace ClinicaPro.UnitTests;

public sealed class MustChangePasswordAccessTests
{
    [Theory]
    [InlineData("/api/auth/change-password")]
    [InlineData("/api/auth/change-password/")]
    [InlineData("/api/auth/me")]
    [InlineData("/API/AUTH/ME")]
    public void Permite_RutasDeCambioDeClaveYPerfil(string path)
    {
        Assert.True(MustChangePasswordAccess.Permite(path));
    }

    [Theory]
    [InlineData("/api/citas")]
    [InlineData("/api/citas/mias")]
    [InlineData("/api/pacientes/me")]
    [InlineData("/hubs/agenda-medico")]
    [InlineData("/api/auth/login")]
    public void Permite_OtrasRutas_EsFalso(string path)
    {
        Assert.False(MustChangePasswordAccess.Permite(path));
    }
}
