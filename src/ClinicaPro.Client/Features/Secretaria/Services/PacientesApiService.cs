namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class PacientesApiService(ApiClient api)
{
    public Task<PaginaPacientesDto> BuscarAsync(
        string? texto,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var url = $"api/pacientes?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(texto))
        {
            url += $"&q={Uri.EscapeDataString(texto.Trim())}";
        }

        return api.ObtenerRequeridoAsync<PaginaPacientesDto>(
            url,
            "No fue posible cargar los pacientes.",
            ct);
    }

    public Task<ResultadoOperacion<PacienteDto>> CrearAsync(
        RegisterPacienteRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<PacienteDto>(
            HttpMethod.Post,
            "api/pacientes",
            request,
            "No fue posible crear el paciente.",
            ct);

    public Task<ResultadoOperacion<PacienteDto>> ActualizarAsync(
        Guid pacienteId,
        ActualizarPerfilRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<PacienteDto>(
            HttpMethod.Put,
            $"api/pacientes/{pacienteId}",
            request,
            "No fue posible actualizar el paciente.",
            ct);
}
