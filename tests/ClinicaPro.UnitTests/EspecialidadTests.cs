using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class EspecialidadTests
{
    [Fact]
    public void Create_ConNombreValido_QuedaActiva()
    {
        var especialidad = Especialidad.Create("  Medicina General  ", "  Atención primaria  ");

        Assert.Equal("Medicina General", especialidad.Nombre);
        Assert.Equal("Atención primaria", especialidad.Descripcion);
        Assert.True(especialidad.IsActive);
        Assert.NotEqual(Guid.Empty, especialidad.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_SinNombre_LanzaExcepcionDeDominio(string? nombre)
    {
        var exception = Assert.Throws<DomainException>(() => Especialidad.Create(nombre!));

        Assert.Contains("obligatorio", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ConNombreDemasiadoLargo_LanzaExcepcionDeDominio()
    {
        var nombre = new string('A', Especialidad.NombreMaxLength + 1);

        var exception = Assert.Throws<DomainException>(() => Especialidad.Create(nombre));

        Assert.Contains("100", exception.Message, StringComparison.Ordinal);
    }
}
