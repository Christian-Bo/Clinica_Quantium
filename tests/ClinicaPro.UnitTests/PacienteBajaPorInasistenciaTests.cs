using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class PacienteBajaPorInasistenciaTests
{
    [Fact]
    public void Exigir_SinCitas_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() => PacienteBajaPorInasistencia.Exigir([]));
        Assert.Contains("primera cita", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Exigir_PrimeraAunNoEsNoPresentada_Lanza()
    {
        var cita = CrearCita();

        var exception = Assert.Throws<DomainException>(() => PacienteBajaPorInasistencia.Exigir([cita]));

        Assert.Contains("No presentada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Exigir_PrimeraNoPresentada_Permite()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.MarcarNoPresentada();

        PacienteBajaPorInasistencia.Exigir([cita]);
    }

    [Fact]
    public void Exigir_YaFueAtendido_Lanza()
    {
        var primera = CrearCita(new DateTime(2027, 3, 15, 9, 0, 0));
        primera.ConfirmarPorSecretaria(Guid.NewGuid());
        primera.MarcarNoPresentada();

        var segunda = CrearCita(new DateTime(2027, 3, 16, 9, 0, 0));
        segunda.ConfirmarPorSecretaria(Guid.NewGuid());
        segunda.ConfirmarAsistencia();
        segunda.RegistrarLlegada();
        segunda.IniciarAtencion();
        segunda.FinalizarAtencion();

        var exception = Assert.Throws<DomainException>(
            () => PacienteBajaPorInasistencia.Exigir([primera, segunda]));

        Assert.Contains("atención", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Cita CrearCita(DateTime? inicio = null)
    {
        return Cita.Solicitar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            inicio ?? new DateTime(2027, 3, 15, 9, 0, 0),
            "Control de presión arterial");
    }
}
