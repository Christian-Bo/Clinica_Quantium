using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class PacienteTests
{
    [Fact]
    public void Create_ConDatosValidos_AsociaElUsuario()
    {
        var usuarioId = Guid.NewGuid();

        var paciente = Paciente.Create(usuarioId, "  Ana  ", "  López  ", "  123456789  ");

        Assert.Equal(usuarioId, paciente.UsuarioId);
        Assert.Equal("Ana", paciente.Nombres);
        Assert.Equal("López", paciente.Apellidos);
        Assert.Equal("Ana López", paciente.NombreCompleto);
        Assert.Equal("123456789", paciente.Documento);
        Assert.True(paciente.IsActive);
    }

    [Fact]
    public void Create_SinNombres_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(
            () => Paciente.Create(Guid.NewGuid(), " ", "López"));

        Assert.Contains("nombres", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_SinUsuario_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(
            () => Paciente.Create(Guid.Empty, "Ana", "López"));

        Assert.Contains("usuario", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Actualizar_CambiaDocumentoYTelefono()
    {
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "López", "111");

        paciente.Actualizar("Ana", "López Prueba", "222", null, "55500000", "Zona 1");

        Assert.Equal("López Prueba", paciente.Apellidos);
        Assert.Equal("222", paciente.Documento);
        Assert.Equal("55500000", paciente.Telefono);
    }
}
