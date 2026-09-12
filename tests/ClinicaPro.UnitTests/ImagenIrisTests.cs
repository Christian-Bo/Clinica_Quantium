using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ImagenIrisTests
{
    private static readonly byte[] ContenidoValido = [1, 2, 3, 4, 5];

    private static ImagenIris Crear(string? lateralidad = null) =>
        ImagenIris.Registrar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "iris.jpg",
            "image/jpeg",
            ContenidoValido,
            lateralidad);

    [Fact]
    public void Registrar_ConDatosValidos_QuedaActiva()
    {
        var imagen = Crear();

        Assert.True(imagen.IsActive);
        Assert.Equal("iris.jpg", imagen.NombreArchivo);
        Assert.Equal("image/jpeg", imagen.TipoContenido);
        Assert.Equal(ContenidoValido, imagen.Imagen);
    }

    [Fact]
    public void Registrar_SinLateralidad_UsaOjoIzquierdo()
    {
        var imagen = Crear();

        Assert.Equal("OI", imagen.Lateralidad);
    }

    [Theory]
    [InlineData("od", "OD")]
    [InlineData("AMBOS", "Ambos")]
    public void Registrar_NormalizaLateralidad(string entrada, string esperado)
    {
        var imagen = Crear(entrada);

        Assert.Equal(esperado, imagen.Lateralidad);
    }

    [Fact]
    public void Registrar_LateralidadInvalida_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() => Crear("ojo derecho"));

        Assert.Contains("lateralidad", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registrar_TipoNoPermitido_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ImagenIris.Registrar(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "documento.pdf",
                "application/pdf",
                ContenidoValido));

        Assert.Contains("image/jpeg", exception.Message);
    }

    [Fact]
    public void Registrar_ArchivoVacio_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() =>
            ImagenIris.Registrar(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "iris.jpg",
                "image/jpeg",
                []));

        Assert.Contains("vacío", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Registrar_ArchivoMuyGrande_Lanza()
    {
        var grande = new byte[ImagenIris.TamanoMaximoBytes + 1];

        var exception = Assert.Throws<DomainException>(() =>
            ImagenIris.Registrar(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "iris.jpg",
                "image/jpeg",
                grande));

        Assert.Contains("10 MB", exception.Message);
    }

    [Fact]
    public void Registrar_SinCita_Lanza()
    {
        Assert.Throws<DomainException>(() =>
            ImagenIris.Registrar(
                Guid.Empty,
                Guid.NewGuid(),
                "iris.jpg",
                "image/jpeg",
                ContenidoValido));
    }

    [Fact]
    public void Registrar_SinUsuario_Lanza()
    {
        Assert.Throws<DomainException>(() =>
            ImagenIris.Registrar(
                Guid.NewGuid(),
                Guid.Empty,
                "iris.jpg",
                "image/jpeg",
                ContenidoValido));
    }

    [Fact]
    public void Desactivar_DosVeces_Lanza()
    {
        var imagen = Crear();
        imagen.Desactivar();

        Assert.False(imagen.IsActive);
        Assert.Throws<DomainException>(() => imagen.Desactivar());
    }
}