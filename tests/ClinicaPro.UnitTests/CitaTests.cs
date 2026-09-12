using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class CitaTests
{
    private static readonly DateTime Inicio = new(2027, 3, 15, 9, 0, 0);

    [Fact]
    public void Solicitar_ConDatosValidos_QuedaSolicitada()
    {
        var cita = CrearCita();

        Assert.Equal(CitaEstados.Solicitada, cita.Estado);
        Assert.Equal(Inicio.AddMinutes(30), cita.FechaHoraFin);
        Assert.Equal(DateTimeKind.Unspecified, cita.FechaHoraInicio.Kind);
    }

    [Fact]
    public void Solicitar_MotivoCorto_LanzaExcepcionDeDominio()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cita.Solicitar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Inicio, "abc"));

        Assert.Contains("motivo", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Solicitar_FechaHoraPasada_LanzaExcepcionDeDominio()
    {
        var ahora = HoraClinica.Ahora();
        var exception = Assert.Throws<DomainException>(() =>
            Cita.Solicitar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ahora.AddMinutes(-5), "Control de presión arterial"));

        Assert.Contains("pasada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExigirNoPasado_HoraDeHoyYaTranscurrida_Lanza()
    {
        var exception = Assert.Throws<DomainException>(() =>
            Cita.ExigirNoPasado(new DateTime(2027, 3, 15, 14, 0, 0), new DateTime(2027, 3, 15, 14, 1, 0)));

        Assert.Contains("pasada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExigirNoPasado_UnMinutoEnElFuturo_NoLanza()
    {
        Cita.ExigirNoPasado(new DateTime(2027, 3, 15, 14, 2, 0), new DateTime(2027, 3, 15, 14, 1, 0));
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
    public void RegistrarLlegada_DesdeProgramada_PasaAEnEspera()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.RegistrarLlegada();

        Assert.Equal(CitaEstados.EnEspera, cita.Estado);
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

        cita.Cancelar(Inicio.AddHours(-3), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.Cancelada, cita.Estado);
    }

    [Fact]
    public void Cancelar_ConMenosDeDosHoras_PasaANoPresentada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.Cancelar(Inicio.AddHours(-1), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.NoPresentada, cita.Estado);
    }

    [Fact]
    public void CancelarPorPaciente_Programada_PasaACancelada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.CancelarPorPaciente(Inicio.AddHours(-3), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.Cancelada, cita.Estado);
    }

    [Fact]
    public void CancelarPorPaciente_Confirmada_LanzaExcepcionDeDominio()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.ConfirmarAsistencia();

        var exception = Assert.Throws<DomainException>(
            () => cita.CancelarPorPaciente(Inicio.AddHours(-3), horasMinimasAnticipacion: 2));

        Assert.Equal(Cita.MensajeCancelacionPacienteConfirmada, exception.Message);
        Assert.Equal(CitaEstados.Confirmada, cita.Estado);
    }

    [Fact]
    public void Cancelar_Staff_Confirmada_PasaACancelada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.ConfirmarAsistencia();

        cita.Cancelar(Inicio.AddHours(-3), horasMinimasAnticipacion: 2);

        Assert.Equal(CitaEstados.Cancelada, cita.Estado);
    }

    [Fact]
    public void Reprogramar_CambiaHorarioYCuenta()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        cita.Reprogramar(Inicio.AddDays(1).AddHours(1), 30, autorizacionAdministradorUsuarioId: null);

        Assert.Equal(Inicio.AddDays(1).AddHours(1), cita.FechaHoraInicio);
        Assert.Equal(1, cita.NumeroReprogramaciones);
        Assert.Equal(CitaEstados.Programada, cita.Estado);
    }

    [Fact]
    public void Reprogramar_AFechaPasada_LanzaExcepcionDeDominio()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());

        var exception = Assert.Throws<DomainException>(() =>
            cita.Reprogramar(HoraClinica.Ahora().AddMinutes(-10), 30, null));

        Assert.Contains("pasada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reprogramar_TerceraSinAdmin_LanzaExcepcion()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.Reprogramar(Inicio.AddDays(1), 30, null);
        cita.Reprogramar(Inicio.AddDays(2), 30, null);

        var exception = Assert.Throws<DomainException>(() =>
            cita.Reprogramar(Inicio.AddDays(3), 30, null));

        Assert.Contains("Administrador", exception.Message);
    }

    [Fact]
    public void Reprogramar_TerceraConAdmin_QuedaAutorizada()
    {
        var cita = CrearCita();
        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        cita.Reprogramar(Inicio.AddDays(1), 30, null);
        cita.Reprogramar(Inicio.AddDays(2), 30, null);
        var adminId = Guid.NewGuid();

        cita.Reprogramar(Inicio.AddDays(3), 30, adminId);

        Assert.Equal(3, cita.NumeroReprogramaciones);
        Assert.Equal(adminId, cita.AutorizacionTerceraPorUsuarioId);
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
            Inicio,
            "Control de presión arterial");
    }
}
