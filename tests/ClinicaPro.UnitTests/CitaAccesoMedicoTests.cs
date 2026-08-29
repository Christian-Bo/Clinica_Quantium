using ClinicaPro.Application.Citas;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class CitaAccesoMedicoTests
{
    [Fact]
    public void ExigirAsignado_MismoMedico_NoLanza()
    {
        var medicoId = Guid.NewGuid();
        var cita = Cita.Solicitar(
            Guid.NewGuid(),
            medicoId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 7, 9, 0, 0),
            "Control de presión arterial");

        CitaAccesoMedico.ExigirAsignado(cita, medicoId);
    }

    [Fact]
    public void ExigirAsignado_OtroMedico_LanzaForbidden()
    {
        var cita = Cita.Solicitar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 7, 9, 0, 0),
            "Control de presión arterial");

        var exception = Assert.Throws<ForbiddenException>(
            () => CitaAccesoMedico.ExigirAsignado(cita, Guid.NewGuid()));

        Assert.Contains("médico asignado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
