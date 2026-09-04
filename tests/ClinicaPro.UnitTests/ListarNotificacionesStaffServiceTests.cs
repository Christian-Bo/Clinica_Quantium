using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ListarNotificacionesStaffServiceTests
{
    [Fact]
    public async Task ExecuteAsync_EstadoInvalido_LanzaExcepcion()
    {
        var servicio = new ListarNotificacionesStaffService(new RepositorioFalso());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(estado: "Inventado"));

        Assert.Contains("estado", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_PasaFiltrosDeEstadoYFechaAlRepositorio()
    {
        var repositorio = new RepositorioFalso();
        var servicio = new ListarNotificacionesStaffService(repositorio);
        var desde = new DateTime(2026, 8, 27);
        var hasta = new DateTime(2026, 8, 27);

        await servicio.ExecuteAsync(NotificacionEstados.Enviada, desde, hasta);

        Assert.Equal(NotificacionEstados.Enviada, repositorio.Estado);
        Assert.Equal(HoraClinica.AUtc(desde), repositorio.DesdeUtc);
        Assert.Equal(HoraClinica.AUtc(hasta.AddDays(1)), repositorio.HastaUtcExclusivo);
        Assert.Equal(100, repositorio.CantidadMaxima);
    }

    private sealed class RepositorioFalso : INotificacionRepository
    {
        public string? Estado { get; private set; }
        public DateTime? DesdeUtc { get; private set; }
        public DateTime? HastaUtcExclusivo { get; private set; }
        public int CantidadMaxima { get; private set; }

        public Task AgregarAsync(Notificacion notificacion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AgregarIntentoAsync(IntentoNotificacion intento, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Notificacion>> ListarPendientesAsync(
            int cantidadMaxima,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notificacion>>([]);

        public Task<IReadOnlyList<Notificacion>> ListarPorPacienteAsync(
            Guid pacienteId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Notificacion>>([]);

        public Task<IReadOnlyList<Notificacion>> ListarStaffAsync(
            string? estado,
            DateTime? desdeUtc,
            DateTime? hastaUtcExclusivo,
            int cantidadMaxima,
            CancellationToken cancellationToken = default)
        {
            Estado = estado;
            DesdeUtc = desdeUtc;
            HastaUtcExclusivo = hastaUtcExclusivo;
            CantidadMaxima = cantidadMaxima;
            return Task.FromResult<IReadOnlyList<Notificacion>>([]);
        }

        public Task<bool> ExistePorCitaYTipoAsync(
            Guid citaId,
            string tipo,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AnularPendientesDeTipoAsync(
            Guid citaId,
            string tipo,
            string motivo,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
