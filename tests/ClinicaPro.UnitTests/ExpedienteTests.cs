using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Contracts.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ExpedienteTests
{
    private static readonly DateTime Inicio = new(2027, 3, 15, 9, 0, 0);

    [Theory]
    [InlineData(CitaEstados.EnEspera, true)]
    [InlineData(CitaEstados.EnAtencion, true)]
    [InlineData(CitaEstados.Programada, false)]
    [InlineData(CitaEstados.Atendida, false)]
    [InlineData(CitaEstados.Solicitada, false)]
    public void PermiteCapturaClinica_SoloSala(string estado, bool esperado)
    {
        Assert.Equal(esperado, CitaEstados.PermiteCapturaClinica(estado));
    }

    [Fact]
    public async Task RegistrarPreconsulta_EnProgramada_Lanza()
    {
        var cita = CitaEnEstado(CitaEstados.Programada);
        var servicio = new RegistrarPreconsultaService(
            new CitasFalso([cita]),
            new PreconsultasFalso(),
            new UnitOfWorkFalso());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(cita.Id, Guid.NewGuid(), Signos()));

        Assert.Contains("En Espera o En Atencion", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegistrarPreconsulta_EnEspera_Guarda()
    {
        var cita = CitaEnEstado(CitaEstados.EnEspera);
        var preconsultas = new PreconsultasFalso();
        var servicio = new RegistrarPreconsultaService(
            new CitasFalso([cita]),
            preconsultas,
            new UnitOfWorkFalso());

        var resultado = await servicio.ExecuteAsync(cita.Id, Guid.NewGuid(), Signos());

        Assert.Equal(cita.Id, resultado.CitaId);
        Assert.Single(preconsultas.Agregadas);
    }

    [Fact]
    public async Task SubirImagenIris_EnAtendida_Lanza()
    {
        var cita = CitaEnEstado(CitaEstados.Atendida);
        var servicio = new SubirImagenIrisService(
            new CitasFalso([cita]),
            new ImagenesFalso(),
            new UnitOfWorkFalso());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(
                cita.Id,
                Guid.NewGuid(),
                new SubirImagenIrisInput("iris.jpg", "image/jpeg", [1, 2, 3, 4], "OI", null)));

        Assert.Contains("En Espera o En Atencion", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccesoExpediente_MedicoSinCitas_LanzaForbidden()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez", "15422");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez");
        var citaDeOtro = Cita.Solicitar(
            paciente.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Inicio,
            "Control de presión arterial");
        var acceso = new AccesoExpedienteService(
            new MedicosFalso(medico),
            new CitasFalso([citaDeOtro]));

        var exception = await Assert.ThrowsAsync<ForbiddenException>(
            () => acceso.ExigirLecturaAsync(usuarioId, esStaffMostrador: false, paciente.Id));

        Assert.Contains("no tiene citas", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExpedientePaciente_MedicoConCita_ListaSignosSinBinario()
    {
        var usuarioId = Guid.NewGuid();
        var medico = Medico.Create(Guid.NewGuid(), usuarioId, "Carlos", "Hernandez", "15422");
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez", alergias: "Penicilina");
        var cita = CitaEnEstado(CitaEstados.EnEspera, paciente.Id, medico.Id);
        var preconsulta = Preconsulta.Registrar(cita.Id, 120, 80, 36.5m, 98m, Guid.NewGuid());
        var servicio = new ObtenerExpedientePacienteService(
            new PacientesFalso(paciente),
            new CitasFalso([cita]),
            new MedicosFalso(medico),
            new PreconsultasFalso([preconsulta]),
            new ImagenesFalso(conteoPorCita: new Dictionary<Guid, int> { [cita.Id] = 2 }),
            new AccesoExpedienteService(new MedicosFalso(medico), new CitasFalso([cita])));

        var resultado = await servicio.ExecuteAsync(paciente.Id, usuarioId, esStaffMostrador: false);

        Assert.NotNull(resultado);
        var visita = Assert.Single(resultado.Visitas);
        Assert.True(visita.Preconsulta is not null);
        Assert.Equal(120, visita.Preconsulta.PresionSistolicaMmHg);
        Assert.Equal(2, visita.CantidadIris);
        Assert.DoesNotContain(
            typeof(ExpedienteVisitaResumenDto).GetProperties(),
            propiedad => propiedad.PropertyType == typeof(byte[]));
        Assert.DoesNotContain(
            typeof(ExpedientePacienteDto).GetProperties(),
            propiedad => propiedad.PropertyType == typeof(byte[]));
    }

    private static RegistrarPreconsultaInput Signos()
        => new(120, 80, 36.5m, 98m, null);

    private static Cita CitaEnEstado(string estado, Guid? pacienteId = null, Guid? medicoId = null)
    {
        var cita = Cita.Solicitar(
            pacienteId ?? Guid.NewGuid(),
            medicoId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Inicio,
            "Control de presión arterial");

        if (estado == CitaEstados.Solicitada)
        {
            return cita;
        }

        cita.ConfirmarPorSecretaria(Guid.NewGuid());
        if (estado == CitaEstados.Programada)
        {
            return cita;
        }

        cita.RegistrarLlegada();
        if (estado == CitaEstados.EnEspera)
        {
            return cita;
        }

        cita.IniciarAtencion();
        if (estado == CitaEstados.EnAtencion)
        {
            return cita;
        }

        cita.FinalizarAtencion();
        return cita;
    }

    private sealed class UnitOfWorkFalso : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesWithSqlSessionContextAsync(
            Guid usuarioId,
            string motivo,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class MedicosFalso(Medico medico) : IMedicoRepository
    {
        public Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult(medico.Id == medicoId ? medico : null);

        public Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default)
            => Task.FromResult<Medico?>(null);

        public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult(usuarioId == medico.UsuarioId ? medico : null);

        public Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([medico]);

        public Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Medico>>([medico]);

        public Task AgregarAsync(Medico item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PacientesFalso(Paciente paciente) : IPacienteRepository
    {
        public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult(paciente.Id == pacienteId ? paciente : null);

        public Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<Paciente?> ObtenerRastreadoPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

        public Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Paciente>>([paciente]);

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
            => Task.FromResult(citas.FirstOrDefault(item => item.Id == citaId));

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
            string? estado = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarQueBloqueanEnRangoAsync(
            Guid medicoId,
            DateTime desde,
            DateTime hasta,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<IReadOnlyList<Cita>> ListarQueBloqueanPacienteEnRangoAsync(
            Guid pacienteId,
            DateTime desde,
            DateTime hasta,
            Guid? exceptoCitaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task<int> ContarActivasFuturasAsync(
            Guid pacienteId,
            DateTime ahoraClinica,
            Guid? exceptoCitaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<Cita>> ListarParaRecordatorioAsync(
            DateTime desdeInicio,
            DateTime hastaInicio,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Cita>>([]);

        public Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class PreconsultasFalso(IReadOnlyList<Preconsulta>? existentes = null) : IPreconsultaRepository
    {
        public List<Preconsulta> Agregadas { get; } = [];

        public Task<Preconsulta?> ObtenerActivaPorCitaAsync(
            Guid citaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult((existentes ?? []).FirstOrDefault(item => item.CitaId == citaId));

        public Task<Preconsulta?> ObtenerRastreadaPorCitaAsync(
            Guid citaId,
            CancellationToken cancellationToken = default)
            => ObtenerActivaPorCitaAsync(citaId, cancellationToken);

        public Task<IReadOnlyList<Preconsulta>> ListarActivasPorCitasAsync(
            IReadOnlyCollection<Guid> citaIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Preconsulta>>(
                (existentes ?? []).Where(item => citaIds.Contains(item.CitaId)).ToList());

        public Task AgregarAsync(Preconsulta preconsulta, CancellationToken cancellationToken = default)
        {
            Agregadas.Add(preconsulta);
            return Task.CompletedTask;
        }
    }

    private sealed class ImagenesFalso(IReadOnlyDictionary<Guid, int>? conteoPorCita = null) : IImagenIrisRepository
    {
        public Task<IReadOnlyList<ImagenIrisMetadato>> ListarPorCitaAsync(
            Guid citaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ImagenIrisMetadato>>([]);

        public Task<ImagenIrisArchivo?> ObtenerArchivoAsync(
            Guid imagenIrisId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ImagenIrisArchivo?>(null);

        public Task<IReadOnlyDictionary<Guid, int>> ContarActivasPorCitasAsync(
            IReadOnlyCollection<Guid> citaIds,
            CancellationToken cancellationToken = default)
            => Task.FromResult(
                (IReadOnlyDictionary<Guid, int>)(conteoPorCita ?? new Dictionary<Guid, int>()));

        public Task<ImagenIris?> ObtenerRastreadaAsync(
            Guid imagenIrisId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ImagenIris?>(null);

        public Task AgregarAsync(ImagenIris imagen, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
