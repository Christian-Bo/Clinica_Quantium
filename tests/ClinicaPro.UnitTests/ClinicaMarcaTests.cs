using ClinicaPro.Domain;

namespace ClinicaPro.UnitTests;

public sealed class ClinicaMarcaTests
{
    [Fact]
    public void NombreOficial_EsClinicaQuantium()
    {
        Assert.Equal("Clínica Quantium", ClinicaMarca.Nombre);
    }

    [Fact]
    public void Remitente_ReemplazaLaMarcaYConservaElCorreo()
    {
        Assert.Equal(
            "Clínica Quantium <noreply@clinica.local>",
            ClinicaMarca.Remitente("Clínica Pro <noreply@clinica.local>"));
        Assert.Equal(
            "Clínica Quantium <correo-real@gmail.com>",
            ClinicaMarca.Remitente("Clínica Pro <correo-real@gmail.com>"));
    }

    [Fact]
    public void Remitente_SinMarcaVieja_NoAlteraElValor()
    {
        Assert.Equal(
            "Clínica Quantium <correo-real@gmail.com>",
            ClinicaMarca.Remitente("Clínica Quantium <correo-real@gmail.com>"));
    }

    [Fact]
    public void Remitente_Vacio_UsaElValorPorDefecto()
    {
        Assert.Equal(ClinicaMarca.RemitentePorDefecto, ClinicaMarca.Remitente("  "));
    }

    [Fact]
    public void Asunto_AnteponeElNombreOficial()
    {
        Assert.Equal("Clínica Quantium — su cita fue programada", ClinicaMarca.Asunto("su cita fue programada"));
    }
}
