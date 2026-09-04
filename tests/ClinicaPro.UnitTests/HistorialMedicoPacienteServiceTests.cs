using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class HistorialMedicoPacienteServiceTests
{
    [Fact]
    public async Task ExecuteAsync_PacienteInexistente_DevuelveNull()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez");
        var servicio = new HistorialMedicoPacienteService(
            new MedicosFalso(medico),
            new PacientesFalso(null),
            new CitasFalso([]));

        var resultado = await servicio.ExecuteAsync(usuarioId, Guid.NewGuid());

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ExecuteAsync_SinCitasConElMedico_LanzaForbidden()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var citaDeOtro = Cita.Solicitar(
            paciente.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 9, 7, 9, 0, 0),
            "Control de presión arterial");
        var servicio = new HistorialMedicoPacienteService(
            new MedicosFalso(medico),
            new PacientesFalso(paciente),
            new CitasFalso([citaDeOtro]));

        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => servicio.ExecuteAsync(usuarioId, paciente.Id));

        Assert.Contains("no tiene citas", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ConCitasDelMedico_DevuelveContextoBasico()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez", alergias: "Penicilina");
        var cita = Cita.Solicitar(
            paciente.Id,
            medico.Id,
            Guid.NewGuid(),
            new DateTime(2026, 9, 7, 9, 0, 0),
            "Control de presión arterial");
        var servicio = new HistorialMedicoPacienteService(
            new MedicosFalso(medico),
            new PacientesFalso(paciente),
            new CitasFalso([cita]));

        var resultado = await servicio.ExecuteAsync(usuarioId, paciente.Id);

        Assert.NotNull(resultado);
        Assert.Equal(paciente.Id, resultado.Paciente.Id);
        Assert.Equal("Penicilina", resultado.Paciente.Alergias);
        Assert.Contains(cita, resultado.CitasProximas.Concat(resultado.CitasPasadas));
    }

    private sealed class MedicosFalso(Medico medico) : IMedicoRepository
    {
        public Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarioId == medico.UsuarioId ? medico : null);

        public Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task AgregarAsync(Medico item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PacientesFalso(Paciente? paciente) : IPacienteRepository
    {
        public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult(paciente is not null && paciente.Id == pacienteId ? paciente : null);

        public Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<Paciente?> ObtenerRastreadoPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Paciente>>([]);

        public Task<string?> ObtenerEmailPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<(IReadOnlyList<Paciente> Items, int Total)> BuscarAsync(
            string? texto,
            int omitir,
            int tomar,
            CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Paciente>, int)>(([], 0));

        public Task<bool> ExisteDocumentoAsync(
            string documento,
            Guid? exceptoPacienteId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Paciente item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CitasFalso(IReadOnlyList<Cita> citas) : ICitaRepository
    {
        public Task<Cita?> ObtenerPorIdAsync(Guid citaId, CancellationToken cancellationToken = default)
            => Task.FromResult<Cita?>(null);

        public Task<IReadOnlyList<Cita>> ListarPorPacienteAsync(
            Guid pacienteId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>(citas.Where(item => item.PacienteId == pacienteId).ToList());

        public Task<IReadOnlyList<Cita>> ListarPorMedicoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarPorEstadoAsync(string estado, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarEnRangoAsync(
            DateTime desde,
            DateTime hasta,
            Guid? medicoId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

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