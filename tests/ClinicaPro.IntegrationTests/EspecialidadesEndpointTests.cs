using System.Net;
using System.Net.Http.Json;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Contracts.Especialidades;
using ClinicaPro.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.IntegrationTests;

public sealed class EspecialidadesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EspecialidadesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsOkWithEspecialidades()
    {
        using var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEspecialidadRepository, RepositorioEspecialidadesDePrueba>();
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/especialidades");
        var payload = await response.Content.ReadFromJsonAsync<List<EspecialidadDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Contains(payload, especialidad => especialidad.Nombre == "Medicina General");
    }

    private sealed class RepositorioEspecialidadesDePrueba : IEspecialidadRepository
    {
        public Task<Especialidad?> ObtenerPorIdAsync(
            Guid especialidadId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Especialidad?>(null);
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
            IReadOnlyList<Especialidad> especialidades = [Especialidad.Create("Medicina General")];
            return Task.FromResult(especialidades);
        }

        public Task<IReadOnlyList<Especialidad>> ListarTodasAsync(
            CancellationToken cancellationToken = default)
        {
            return ListarActivasAsync(cancellationToken);
        }

        public Task AgregarAsync(Especialidad especialidad, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
