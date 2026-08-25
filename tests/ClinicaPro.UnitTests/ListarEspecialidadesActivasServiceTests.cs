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
        public Task<IReadOnlyList<Especialidad>> ListarActivasAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(especialidades);
        }
    }
}
