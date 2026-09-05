using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class SolicitarCitaServiceTests
{
    private static readonly DateTime FechaFutura = new(2027, 3, 15, 9, 0, 0);

    private static SolicitarCitaService Construir(Medico? medico)
    {
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var citas = new CitasFalso();
        var parametros = new ParametrosFalso();
        return new SolicitarCitaService(
            new PacientesFalso(paciente),
            new MedicosFalso(medico),
            citas,
            parametros,
            new ValidarAgendaPacienteService(citas, parametros),
            new UnitOfWorkFalso(),
            null!);
    }

    [Fact]
    public async Task ExecuteAsync_MedicoNoExiste_LanzaDomainException()
    {
        var servicio = Construir(medico: null);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(
                Guid.NewGuid(),
                new SolicitarCitaInput(Guid.NewGuid(), FechaFutura, "Dolor de cabeza")));

        Assert.Contains("no existe", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CrearPendienteAsync_MedicoActivo_AgregaCitaDelMedico()
    {
        var medico = Medico.Create(Guid.NewGuid(), Guid.NewGuid(), "Carlos", "Hernandez");
        var citas = new CitasFalso();
        var parametros = new ParametrosFalso();
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var servicio = new SolicitarCitaService(
            new PacientesFalso(paciente),
            new MedicosFalso(medico),
            citas,
            parametros,
            new ValidarAgendaPacienteService(citas, parametros),
            new UnitOfWorkFalso(),
            null!);

        var cita = await servicio.CrearPendienteAsync(
            paciente.Id,
            paciente.UsuarioId,
            new SolicitarCitaInput(medico.Id, FechaFutura, "Dolor de cabeza"));

        Assert.Equal(medico.Id, cita.MedicoId);
        Assert.Equal(paciente.Id, cita.PacienteId);
        Assert.Equal("Dolor de cabeza", cita.MotivoConsulta);
        Assert.Single(citas.Agregadas);
    }

    [Fact]
    public async Task CrearPendienteAsync_MotivoCorto_LanzaDomainException()
    {
        var medico = Medico.Create(Guid.NewGuid(), Guid.NewGuid(), "Carlos", "Hernandez");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var citas = new CitasFalso();
        var parametros = new ParametrosFalso();
        var servicio = new SolicitarCitaService(
            new PacientesFalso(paciente),
            new MedicosFalso(medico),
            citas,
            parametros,
            new ValidarAgendaPacienteService(citas, parametros),
            new UnitOfWorkFalso(),
            null!);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.CrearPendienteAsync(
                paciente.Id,
                paciente.UsuarioId,
                new SolicitarCitaInput(medico.Id, FechaFutura, "abc")));

        Assert.Contains("motivo", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(citas.Agregadas);
    }

    [Fact]
    public async Task CrearPendienteAsync_PacienteYaTieneEseHorario_LanzaDomainException()
    {
        var medico = Medico.Create(Guid.NewGuid(), Guid.NewGuid(), "Carlos", "Hernandez");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var citas = new CitasFalso
        {
            Existentes =
            {
                Cita.Solicitar(paciente.Id, Guid.NewGuid(), paciente.UsuarioId, FechaFutura, "Control previo")
            }
        };
        var parametros = new ParametrosFalso();
        var servicio = new SolicitarCitaService(
            new PacientesFalso(paciente),
            new MedicosFalso(medico),
            citas,
            parametros,
            new ValidarAgendaPacienteService(citas, parametros),
            new UnitOfWorkFalso(),
            null!);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.CrearPendienteAsync(
                paciente.Id,
                paciente.UsuarioId,
                new SolicitarCitaInput(medico.Id, FechaFutura, "Dolor de cabeza")));

        Assert.Contains("ya tiene una cita", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(citas.Agregadas);
    }

    [Fact]
    public async Task ExecuteAsync_MedicoInactivo_LanzaDomainException()
    {
        var medico = Medico.Create(Guid.NewGuid(), Guid.NewGuid(), "Carlos", "Hernandez");
        var servicio = Construir(medico: null);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(
                Guid.NewGuid(),
                new SolicitarCitaInput(medico.Id, FechaFutura, "Dolor de cabeza")));

        Assert.Contains("activo", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MedicosFalso(Medico? medico) : IMedicoRepository
    {
        public Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult(medico is not null && medico.Id == medicoId ? medico : null);

        public Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([]);

        public Task AgregarAsync(Medico item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PacientesFalso(Paciente paciente) : IPacienteRepository
    {
        public Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<Paciente?> ObtenerRastreadoPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Paciente>>([]);

        public Task<string?> ObtenerEmailPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<(IReadOnlyList<Paciente> Items, int Total)> BuscarAsync(string? termino, int pagina, int tamanio, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Paciente>, int)>(([], 0));

        public Task<bool> ExisteDocumentoAsync(string documento, Guid? exceptoPacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class ParametrosFalso : IParametroRepository
    {
        public Task<IReadOnlyList<Parametro>> ListarActivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Parametro>>([]);

        public Task<int> ObtenerEnteroAsync(string clave, int valorPredeterminado, CancellationToken cancellationToken = default)
            => Task.FromResult(valorPredeterminado);

        public Task<Parametro?> ObtenerRastreadoAsync(string clave, CancellationToken cancellationToken = default)
            => Task.FromResult<Parametro?>(null);
    }

    private sealed class CitasFalso : ICitaRepository
    {
        public List<Cita> Agregadas { get; } = [];
        public List<Cita> Existentes { get; } = [];

        public Task<Cita?> ObtenerPorIdAsync(Guid citaId, CancellationToken cancellationToken = default)
            => Task.FromResult<Cita?>(null);

        public Task<IReadOnlyList<Cita>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarPorMedicoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarPorEstadoAsync(string estado, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarEnRangoAsync(DateTime desde, DateTime hasta, Guid? medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarQueBloqueanEnRangoAsync(Guid medicoId, DateTime desde, DateTime hasta, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarQueBloqueanPacienteEnRangoAsync(
            Guid pacienteId, DateTime desde, DateTime hasta, Guid? exceptoCitaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>(
                Existentes.Where(cita =>
                    cita.PacienteId == pacienteId
                    && CitaEstados.BloqueaHorario(cita.Estado)
                    && cita.FechaHoraInicio < hasta
                    && desde < cita.FechaHoraFin
                    && cita.Id != exceptoCitaId).ToList());

        public Task<int> ContarActivasFuturasAsync(
            Guid pacienteId, DateTime ahoraClinica, Guid? exceptoCitaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Existentes.Count(cita =>
                cita.PacienteId == pacienteId
                && CitaEstados.BloqueaHorario(cita.Estado)
                && cita.FechaHoraInicio >= ahoraClinica
                && cita.Id != exceptoCitaId));

        public Task<IReadOnlyList<Cita>> ListarParaRecordatorioAsync(DateTime desdeInicio, DateTime hastaInicio, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
        {
            Agregadas.Add(cita);
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkFalso : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesWithSqlSessionContextAsync(Guid usuarioId, string motivo, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}