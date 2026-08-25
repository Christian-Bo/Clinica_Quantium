using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class MedicoTests
{
    [Fact]
    public void Create_ConDatosValidos_ActivaElMedico()
    {
        var usuarioId = Guid.NewGuid();

        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "  Carlos  ", "  Hernandez  ", "COL-1", "555");

        Assert.Equal(usuarioId, medico.UsuarioId);
        Assert.Equal("Carlos", medico.Nombres);
        Assert.Equal("Carlos Hernandez", medico.NombreCompleto);
        Assert.True(medico.IsActive);
    }

    [Fact]
    public void Create_SinUsuario_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(
            () => Medico.Create(Guid.NewGuid(), Guid.Empty, "Carlos", "Hernandez"));

        Assert.Contains("usuario", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Horario_DiaInvalido_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(
            () => Horario.Create(Guid.NewGuid(), 8, new TimeOnly(8, 0), new TimeOnly(16, 0)));

        Assert.Contains("día", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
