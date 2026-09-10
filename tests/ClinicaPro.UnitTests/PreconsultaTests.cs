using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class PreconsultaTests
{
    private static Preconsulta Crear() =>
        Preconsulta.Registrar(Guid.NewGuid(), 120, 80, 36.5m, 98m, Guid.NewGuid());

    [Fact]
    public void Registrar_ConDatosValidos_QuedaActiva()
    {
        var preconsulta = Crear();

        Assert.True(preconsulta.IsActive);
        Assert.Equal(120, preconsulta.PresionSistolicaMmHg);
        Assert.Equal(36.5m, preconsulta.TemperaturaCelsius);
        Assert.Null(preconsulta.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(50, 40)]
    [InlineData(300, 80)]
    public void Registrar_PresionSistolicaFueraDeRango_Lanza(short sistolica, short diastolica)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Preconsulta.Registrar(Guid.NewGuid(), sistolica, diastolica, 36.5m, 98m, Guid.NewGuid()));

        Assert.Contains("sistólica", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registrar_DiastolicaMayorQueSistolica_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Preconsulta.Registrar(Guid.NewGuid(), 90, 120, 36.5m, 98m, Guid.NewGuid()));

        Assert.Contains("menor que la sistólica", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(50)]
    public void Registrar_TemperaturaFueraDeRango_Lanza(decimal temperatura)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Preconsulta.Registrar(Guid.NewGuid(), 120, 80, temperatura, 98m, Guid.NewGuid()));

        Assert.Contains("temperatura", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(105)]
    public void Registrar_OxigenoFueraDeRango_Lanza(decimal oxigeno)
    {
        var exception = Assert.Throws<DomainException>(() =>
            Preconsulta.Registrar(Guid.NewGuid(), 120, 80, 36.5m, oxigeno, Guid.NewGuid()));

        Assert.Contains("oxígeno", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registrar_SinCita_Lanza()
    {
        Assert.Throws<DomainException>(() =>
            Preconsulta.Registrar(Guid.Empty, 120, 80, 36.5m, 98m, Guid.NewGuid()));
    }

    [Fact]
    public void Actualizar_CambiaValoresYMarcaFecha()
    {
        var preconsulta = Crear();
        var otroUsuario = Guid.NewGuid();

        preconsulta.Actualizar(130, 85, 37.2m, 96m, otroUsuario, "Paciente con tos");

        Assert.Equal(130, preconsulta.PresionSistolicaMmHg);
        Assert.Equal(37.2m, preconsulta.TemperaturaCelsius);
        Assert.Equal("Paciente con tos", preconsulta.Observacion);
        Assert.Equal(otroUsuario, preconsulta.RegistradaPorUsuarioId);
        Assert.NotNull(preconsulta.UpdatedAtUtc);
    }

    [Fact]
    public void Actualizar_SobreInactiva_Lanza()
    {
        var preconsulta = Crear();
        preconsulta.Desactivar();

        Assert.Throws<DomainException>(() =>
            preconsulta.Actualizar(130, 85, 37m, 97m, Guid.NewGuid()));
    }

    [Fact]
    public void Desactivar_DosVeces_Lanza()
    {
        var preconsulta = Crear();
        preconsulta.Desactivar();

        Assert.Throws<DomainException>(() => preconsulta.Desactivar());
    }
}