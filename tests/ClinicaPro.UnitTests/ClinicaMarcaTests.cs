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

    [Fact]
    public void CuerpoHtml_IncluyeMarcaYConvierteParrafos()
    {
        var html = ClinicaMarca.CuerpoHtml("Hola Ana,\n\nSu cita quedó programada.");

        Assert.Contains("Clínica Quantium", html, StringComparison.Ordinal);
        Assert.Contains("<p style=\"margin:0 0 1rem 0;\">Hola Ana,</p>", html, StringComparison.Ordinal);
        Assert.Contains("No responda a esta dirección.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Hola Ana,\n\nSu cita", html, StringComparison.Ordinal);
    }

    [Fact]
    public void CuerpoHtml_EscapaHtmlDelContenido()
    {
        var html = ClinicaMarca.CuerpoHtml("Hola <script>alert(1)</script>");

        Assert.Contains("Hola &lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>alert(1)</script>", html, StringComparison.Ordinal);
    }
}
