using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;

namespace ClinicaPro.UnitTests;

public sealed class NotificacionTests
{
    [Theory]
    [InlineData(CitaEstados.Solicitada, NotificacionTipos.SolicitudRecibida)]
    [InlineData(CitaEstados.Programada, NotificacionTipos.CitaProgramada)]
    [InlineData(CitaEstados.Confirmada, NotificacionTipos.CitaConfirmada)]
    [InlineData(CitaEstados.Rechazada, NotificacionTipos.CitaRechazada)]
    [InlineData(CitaEstados.Cancelada, NotificacionTipos.CitaCancelada)]
    [InlineData(CitaEstados.NoPresentada, NotificacionTipos.CitaNoPresentada)]
    [InlineData(CitaEstados.EnEspera, null)]
    [InlineData(CitaEstados.Atendida, null)]
    public void DesdeEstadoCita_MapeaSoloEstadosQueAvisanAlPaciente(string estado, string? esperado)
    {
        Assert.Equal(esperado, NotificacionTipos.DesdeEstadoCita(estado));
    }

    [Fact]
    public void EncolarEmail_QuedaPendienteEnCanalEmail()
    {
        var aviso = Notificacion.EncolarEmail(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificacionTipos.CitaProgramada,
            "ana@clinica.test",
            "Asunto",
            "Cuerpo");

        Assert.Equal(NotificacionEstados.Pendiente, aviso.Estado);
        Assert.Equal(NotificacionCanales.Email, aviso.Canal);
        Assert.Equal(0, aviso.NumeroIntentos);
    }

    [Fact]
    public void MarcarEnviada_CierraElAviso()
    {
        var aviso = Notificacion.EncolarEmail(
            Guid.NewGuid(),
            Guid.NewGuid(),
            NotificacionTipos.CitaProgramada,
            "ana@clinica.test",
            "Asunto",
            "Cuerpo");

        aviso.MarcarProcesando();
        aviso.MarcarEnviada();

        Assert.Equal(NotificacionEstados.Enviada, aviso.Estado);
        Assert.Equal(1, aviso.NumeroIntentos);
        Assert.NotNull(aviso.EnviadaAtUtc);
    }
}
