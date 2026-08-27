using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.UnitTests;

public sealed class BuscarPacientesServiceTests
{
    [Fact]
    public async Task ExecuteAsync_PaginaLosResultadosYDevuelveElTotal()
    {
        var servicio = new BuscarPacientesService(new RepositorioFalso(
        [
            Paciente.Create(Guid.NewGuid(), "Ana", "Lopez"),
            Paciente.Create(Guid.NewGuid(), "Beto", "Martinez"),
            Paciente.Create(Guid.NewGuid(), "Carla", "Nunez")
        ]));

        var resultado = await servicio.ExecuteAsync(texto: null, page: 2, pageSize: 1);

        Assert.Equal(3, resultado.Total);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(1, resultado.PageSize);
        Assert.Single(resultado.Items);
        Assert.Equal("Martinez", resultado.Items[0].Apellidos);
    }

    [Fact]
    public async Task ExecuteAsync_PaginaInvalida_LanzaExcepcion()
    {
        var servicio = new BuscarPacientesService(new RepositorioFalso([]));

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => servicio.ExecuteAsync(null, page: 0));

        Assert.Contains("página", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RepositorioFalso(IReadOnlyList<Paciente> pacientes) : IPacienteRepository
    {
        public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
            => Task.FromResult<Paciente?>(null);

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
        {
            var ordenados = pacientes
                .OrderBy(item => item.Apellidos)
                .ThenBy(item => item.Nombres)
                .ToList();
            return Task.FromResult<(IReadOnlyList<Paciente>, int)>((
                ordenados.Skip(omitir).Take(tomar).ToList(),
                ordenados.Count));
        }

        public Task<bool> ExisteDocumentoAsync(
            string documento,
            Guid? exceptoPacienteId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
