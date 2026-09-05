using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClinicaPro.IntegrationTests;

public sealed class AuthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PacienteMe_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pacientes/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterPaciente_WithoutPrimeraCita_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var body = JsonContent.Create(new
        {
            email = "nuevo@clinica.com",
            password = "Paciente123!",
            nombres = "Ana",
            apellidos = "Lopez"
        });

        var response = await client.PostAsync("/api/auth/register/paciente", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var texto = await response.Content.ReadAsStringAsync();
        Assert.Contains("primera cita", texto, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterPaciente_WithEmptyMedicoId_ReturnsBadRequest()
    {
        using var client = _factory.CreateClient();
        var body = JsonContent.Create(new
        {
            email = "nuevo@clinica.com",
            password = "Paciente123!",
            nombres = "Ana",
            apellidos = "Lopez",
            medicoId = Guid.Empty,
            fechaHoraInicio = "2027-03-15T09:00:00",
            motivoConsulta = "Dolor de cabeza persistente"
        });

        var response = await client.PostAsync("/api/auth/register/paciente", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/change-password", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
