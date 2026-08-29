using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class AutorizacionReprogramacionTests
{
    [Fact]
    public void Solicitar_MotivoCorto_LanzaExcepcion()
    {
        var exception = Assert.Throws<DomainException>(
            () => AutorizacionReprogramacion.Solicitar(Guid.NewGuid(), Guid.NewGuid(), "abc"));

        Assert.Contains("motivo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Aprobar_DesdePendiente_QuedaAprobada()
    {
        var adminId = Guid.NewGuid();
        var autorizacion = AutorizacionReprogramacion.Solicitar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tercera reprogramación del paciente");

        autorizacion.Aprobar(adminId, "De acuerdo");

        Assert.Equal(AutorizacionReprogramacionEstados.Aprobada, autorizacion.Estado);
        Assert.Equal(adminId, autorizacion.AutorizadaPorUsuarioId);
    }

    [Fact]
    public void Rechazar_YaAprobada_LanzaExcepcion()
    {
        var autorizacion = AutorizacionReprogramacion.Solicitar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Tercera reprogramación del paciente");
        autorizacion.Aprobar(Guid.NewGuid(), null);

        var exception = Assert.Throws<DomainException>(
            () => autorizacion.Rechazar(Guid.NewGuid(), "tarde"));

        Assert.Contains("ya fue resuelta", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
