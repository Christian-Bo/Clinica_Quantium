using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ValidarAgendaPacienteServiceTests
{
    private static readonly DateTime Inicio = new(2027, 3, 15, 9, 0, 0);
    private static readonly DateTime Fin = Inicio.AddMinutes(30);

    [Fact]
    public async Task ExigirPuedeAgendarAsync_SinCitas_NoLanza()
    {
        var servicio = new ValidarAgendaPacienteService(new CitasFalso(), new ParametrosFalso());

        await servicio.ExigirPuedeAgendarAsync(
            Guid.NewGuid(), Inicio, Fin, exceptoCitaId: null, cuentaComoCitaNueva: true);
    }

    [Fact]
    public async Task ExigirPuedeAgendarAsync_SolapaConOtraCita_Lanza()
    {
        var pacienteId = Guid.NewGuid();
        var ocupada = Cita.Solicitar(pacienteId, Guid.NewGuid(), Guid.NewGuid(), Inicio, "Control general");
        var citas = new CitasFalso { Existentes = [ocupada] };
        var servicio = new ValidarAgendaPacienteService(citas, new ParametrosFalso());

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExigirPuedeAgendarAsync(
                pacienteId, Inicio, Fin, exceptoCitaId: null, cuentaComoCitaNueva: true));

        Assert.Contains("ya tiene una cita", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExigirPuedeAgendarAsync_ReprogramarMismaCita_NoCuentaComoSolape()
    {
        var pacienteId = Guid.NewGuid();
        var propia = Cita.Solicitar(pacienteId, Guid.NewGuid(), Guid.NewGuid(), Inicio, "Control general");
        var citas = new CitasFalso { Existentes = [propia] };
        var servicio = new ValidarAgendaPacienteService(citas, new ParametrosFalso());

        await servicio.ExigirPuedeAgendarAsync(
            pacienteId, Inicio, Fin, propia.Id, cuentaComoCitaNueva: false);
    }

    [Fact]
    public async Task ExigirPuedeAgendarAsync_TresActivasFuturas_LanzaAlPedirOtra()
    {
        var pacienteId = Guid.NewGuid();
        var citas = new CitasFalso
        {
            Existentes =
            [
                Cita.Solicitar(pacienteId, Guid.NewGuid(), Guid.NewGuid(), Inicio, "Primera consulta"),
                Cita.Solicitar(pacienteId, Guid.NewGuid(), Guid.NewGuid(), Inicio.AddHours(1), "Segunda consulta"),
                Cita.Solicitar(pacienteId, Guid.NewGuid(), Guid.NewGuid(), Inicio.AddHours(2), "Tercera consulta")
            ]
        };
        var servicio = new ValidarAgendaPacienteService(citas, new ParametrosFalso());

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExigirPuedeAgendarAsync(
                pacienteId,
                Inicio.AddHours(3),
                Inicio.AddHours(3).AddMinutes(30),
                exceptoCitaId: null,
                cuentaComoCitaNueva: true));

        Assert.Contains("más de 3", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CitasFalso : ICitaRepository
    {
        public List<Cita> Existentes { get; init; } = [];

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
}
