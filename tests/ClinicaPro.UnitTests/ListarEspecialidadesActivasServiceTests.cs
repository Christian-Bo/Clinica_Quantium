using ClinicaPro.Application.Especialidades;
using ClinicaPro.Domain.Entities;

namespace ClinicaPro.UnitTests;

public sealed class ListarEspecialidadesActivasServiceTests
{
    [Fact]
    public async Task ExecuteAsync_DevuelveLasEspecialidadesDelRepositorio()
    {
        var repositorio = new RepositorioFalso(
        [
            Especialidad.Create("Cardiología"),
            Especialidad.Create("Dermatología")
        ]);
        var servicio = new ListarEspecialidadesActivasService(repositorio);

        var resultado = await servicio.ExecuteAsync();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, especialidad => especialidad.Nombre == "Cardiología");
        Assert.Contains(resultado, especialidad => especialidad.Nombre == "Dermatología");
    }

    private sealed class RepositorioFalso(IReadOnlyList<Especialidad> especialidades) : IEspecialidadRepository
    {
        public Task<Especialidad?> ObtenerPorIdAsync(
            Guid especialidadId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(especialidades.FirstOrDefault(item => item.Id == especialidadId));
        }

        public Task<Especialidad?> ObtenerRastreadaAsync(
            Guid especialidadId,
            CancellationToken cancellationToken = default)
        {
            return ObtenerPorIdAsync(especialidadId, cancellationToken);
        }

        public Task<IReadOnlyList<Especialidad>> ListarActivasAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(especialidades);
        }

        public Task<IReadOnlyList<Especialidad>> ListarTodasAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(especialidades);
        }

        public Task AgregarAsync(Especialidad especialidad, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
