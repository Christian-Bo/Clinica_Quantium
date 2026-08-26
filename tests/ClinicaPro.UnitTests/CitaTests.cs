using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class CitaTests
{
    [Fact]
    public void Solicitar_ConDatosValidos_QuedaSolicitada()
    {
        var cita = CrearCita();

        Assert.Equal(CitaEstados.Solicitada, cita.Estado);
        Assert.Equal(new DateTime(2026, 9, 7, 9, 30, 0), cita.FechaHoraFin);
        Assert.Equal(DateTimeKind.Unspecified, cita.FechaHoraInicio.Kind);
    }

    [Fact]
    public void Solicitar_MotivoCorto_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cita.Solicitar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                new DateTime(2026, 9, 7, 9, 0, 0), "abc"));

        Assert.Contains("motivo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfirmarPorSecretaria_DesdeSolicitada_PasaAProgramada()
    {
        var cita = CrearCita();
        var secretariaId = Guid.NewGuid();

        cita.ConfirmarPorSecretaria(secretariaId);

        Assert.Equal(CitaEstados.Programada, cita.Estado);
        Assert.Equal(secretariaId, cita.SecretariaResponsableId);
    }

    [Fact]
    public void ConfirmarPorSecretaria_DesdeOtroEstado_LanzaExcepcion()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        var exception = Assert.Throws<DomainException>(() => cita.ConfirmarPorSecretaria(Guid.NewGuid()));

        Assert.Contains("Solicitada", exception.Message);
    }

    [Fact]
    public void FlujoRecepcion_HastaAtendida_RespetaLaMaquinaDeEstados()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.ConfirmarAsistencia();
        cita.RegistrarLlegada();
        cita.IniciarAtencion();
        cita.FinalizarAtencion();

        Assert.Equal(CitaEstados.Atendida, cita.Estado);
    }

    [Fact]
    public void Cancelar_ConMasDeDosHoras_PasaACancelada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.Cancelar(new DateTime(2026, 9, 7, 6, 0, 0), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.Cancelada, cita.Estado);
    }

    [Fact]
    public void Cancelar_ConMenosDeDosHoras_PasaANoPresentada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.Cancelar(new DateTime(2026, 9, 7, 8, 0, 0), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.NoPresentada, cita.Estado);
    }

    [Fact]
    public void Reprogramar_CambiaHorarioYCuenta()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.Reprogramar(new DateTime(2026, 9, 8, 10, 0, 0), 30, autorizacionAdministradorUsuarioId: null);

        Assert.Equal(new DateTime(2026, 9, 8, 10, 0, 0), cita.FechaHoraInicio);
        Assert.Equal(1, cita.NumeroReprogramaciones);
        Assert.Equal(CitaEstados.Programada, cita.Estado);
    }

    [Fact]
    public void Reprogramar_TerceraSinAdmin_LanzaExcepcion()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.Reprogramar(new DateTime(2026, 9, 8, 10, 0, 0), 30, null);
        cita.Reprogramar(new DateTime(2026, 9, 9, 10, 0, 0), 30, null);

        var exception = Assert.Throws<DomainException>(() =>
            cita.Reprogramar(new DateTime(2026, 9, 10, 10, 0, 0), 30, null));

        Assert.Contains("Administrador", exception.Message);
    }

    [Fact]
    public void DiaSemana_LunesEsUnoYDomingoEsSiete()
    {
        Assert.Equal(1, HoraClinica.DiaSemana(new DateTime(2026, 9, 7)));
        Assert.Equal(7, HoraClinica.DiaSemana(new DateTime(2026, 9, 13)));
    }

    private static Cita CrearCita()
    {
        return Cita.Solicitar(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 7, 9, 0, 0),
            "Control de presión arterial");
    }
}
