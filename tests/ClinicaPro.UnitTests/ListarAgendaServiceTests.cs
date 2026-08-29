using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ListarAgendaServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Medico_IgnoraMedicoIdAjenoYUsaElPropio()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez");
        var citas = new CitasFalso();
        var servicio = new ListarAgendaService(citas, new MedicosFalso(medico));

        await servicio.ExecuteAsync(
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 8),
            medicoId: Guid.NewGuid(),
            usuarioId,
            soloAgendaPropia: true);

        Assert.Equal(medico.Id, citas.MedicoIdFiltrado);
    }

    [Fact]
    public async Task ExecuteAsync_Staff_RespetaMedicoIdExterno()
    {
        var medicoAjeno = Guid.NewGuid();
        var citas = new CitasFalso();
        var servicio = new ListarAgendaService(citas, new MedicosFalso(null));

        await servicio.ExecuteAsync(
            new DateTime(2026, 9, 7),
            new DateTime(2026, 9, 8),
            medicoAjeno,
            Guid.NewGuid(),
            soloAgendaPropia: false);

        Assert.Equal(medicoAjeno, citas.MedicoIdFiltrado);
    }

    [Fact]
    public async Task ExecuteAsync_MedicoSinPerfil_LanzaExcepcion()
    {
        var servicio = new ListarAgendaService(new CitasFalso(), new MedicosFalso(null));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(
                new DateTime(2026, 9, 7),
                new DateTime(2026, 9, 8),
                Guid.NewGuid(),
                Guid.NewGuid(),
                soloAgendaPropia: true));

        Assert.Contains("médico", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MedicosFalso(Medico? medico) : IMedicoRepository
    {
        public Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(medico is not null && medico.UsuarioId == usuarioId ? medico : null);

        public Task<Medico?> ObtenerPrimarioPorEspecialidadAsync(
            Guid especialidadId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task<IReadOnlyList<MedicoEspecialidad>> ListarEspecialidadesActivasAsync(
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MedicoEspecialidad>>([]);

        public Task AgregarAsync(Medico item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task AgregarEspecialidadAsync(MedicoEspecialidad relacion, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CitasFalso : ICitaRepository
    {
        public Guid? MedicoIdFiltrado { get; private set; }

        public Task<Cita?> ObtenerPorIdAsync(Guid citaId, CancellationToken cancellationToken = default)
            => Task.FromResult<Cita?>(null);

        public Task<IReadOnlyList<Cita>> ListarPorPacienteAsync(
            Guid pacienteId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarPorMedicoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarPorEstadoAsync(string estado, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarEnRangoAsync(
            DateTime desde,
            DateTime hasta,
            Guid? medicoId,
            CancellationToken cancellationToken = default)
        {
            MedicoIdFiltrado = medicoId;
            return Task.FromResult<IReadOnlyList<Cita>>([]);
        }

        public Task<IReadOnlyList<Cita>> ListarQueBloqueanEnRangoAsync(
            Guid medicoId,
            DateTime desde,
            DateTime hasta,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarParaRecordatorioAsync(
            DateTime desdeInicio,
            DateTime hastaInicio,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
