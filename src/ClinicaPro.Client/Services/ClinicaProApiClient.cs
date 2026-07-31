using System.Net.Http.Json;
using ClinicaPro.Contracts.Appointments;
using ClinicaPro.Contracts.Catalogs;
using ClinicaPro.Contracts.Common;

namespace ClinicaPro.Client.Services;

public sealed class ClinicaProApiClient
{
    private readonly HttpClient _httpClient;

    public ClinicaProApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/health", cancellationToken);
        response.EnsureSuccessStatusCode();
        return "OK";
    }

    public async Task<IReadOnlyCollection<SpecialtyResponse>> GetSpecialtiesAsync(
        CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<IReadOnlyCollection<SpecialtyResponse>>(
            "api/specialties",
            cancellationToken)
        ?? Array.Empty<SpecialtyResponse>();

    public async Task<AppointmentResponse> RequestAppointmentAsync(
        RequestAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/appointments/requests",
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AppointmentResponse>(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("El backend no devolvió la solicitud creada.");
        }

        var problem = await response.Content.ReadFromJsonAsync<ApiProblem>(cancellationToken: cancellationToken);
        throw new InvalidOperationException(problem?.Detail ?? "No fue posible registrar la solicitud.");
    }
}
