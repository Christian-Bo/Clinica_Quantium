using ClinicaPro.Application;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class ActualizarPerfilPacienteServiceTests
{
    [Fact]
    public async Task ExecuteAsync_PacienteCambiaDpi_LanzaDomainException()
    {
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez", "1234567890101");
        var servicio = new ActualizarPerfilPacienteService(
            new PacientesFalso(paciente),
            new UnitOfWorkFalso(),
            new AuditoriaFalsa());

        var error = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(
                paciente.UsuarioId,
                "Ana",
                "Lopez",
                "9999999990101",
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.Contains("documento", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("1234567890101", paciente.Documento);
    }

    [Fact]
    public async Task ExecuteAsync_MismoDpi_ConservaDocumento()
    {
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez", "1234567890101");
        var servicio = new ActualizarPerfilPacienteService(
            new PacientesFalso(paciente),
            new UnitOfWorkFalso(),
            new AuditoriaFalsa());

        var actualizado = await servicio.ExecuteAsync(
            paciente.UsuarioId,
            "Ana",
            "Lopez Perez",
            "1234567890101",
            null,
            "55500000",
            null,
            null,
            null,
            null,
            null);

        Assert.Equal("1234567890101", actualizado.Documento);
        Assert.Equal("Lopez Perez", actualizado.Apellidos);
    }

    [Fact]
    public async Task ExecutePorPacienteIdAsync_StaffPuedeCorregirDpi()
    {
        var paciente = Paciente.Create(Guid.NewGuid(), "Ana", "Lopez", "1234567890101");
        var servicio = new ActualizarPerfilPacienteService(
            new PacientesFalso(paciente),
            new UnitOfWorkFalso(),
            new AuditoriaFalsa());

        var actualizado = await servicio.ExecutePorPacienteIdAsync(
            Guid.NewGuid(),
            paciente.Id,
            "Ana",
            "Lopez",
            "1111111110101",
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        Assert.Equal("1111111110101", actualizado.Documento);
    }

    private sealed class PacientesFalso(Paciente paciente) : IPacienteRepository
    {
        public Task<Paciente?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<Paciente?> ObtenerRastreadoPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(paciente);

        public Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Paciente>>([]);

        public Task<string?> ObtenerEmailPorPacienteIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);

        public Task<(IReadOnlyList<Paciente> Items, int Total)> BuscarAsync(string? termino, int pagina, int tamanio, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Paciente>, int)>(([], 0));

        public Task<bool> ExisteDocumentoAsync(string documento, Guid? exceptoPacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Paciente item, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UnitOfWorkFalso : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task SaveChangesWithSqlSessionContextAsync(Guid usuarioId, string motivo, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class AuditoriaFalsa : IAuditoriaWriter
    {
        public Task RegistrarAsync(
            Guid? usuarioId,
            string accion,
            string entidad,
            string? entidadId,
            string? detalle,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RegistroAuditoria>> ListarRecientesAsync(
            int cantidadMaxima,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RegistroAuditoria>>([]);
    }
}
