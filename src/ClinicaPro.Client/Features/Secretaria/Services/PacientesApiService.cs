namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class PacientesApiService(HttpClient http)
{
    public async Task<PaginaPacientesDto> BuscarAsync(
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

        return await http.GetFromJsonAsync<PaginaPacientesDto>(url, ct)
            ?? new PaginaPacientesDto([], 0, page, pageSize);
    }

    public async Task<ResultadoOperacion<PacienteDto>> CrearAsync(
        RegisterPacienteRequest request,
        CancellationToken ct = default)
    {
        var respuesta = await http.PostAsJsonAsync("api/pacientes", request, ct);

        if (respuesta.IsSuccessStatusCode)
        {
            var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteDto>(cancellationToken: ct);
            return paciente is not null
                ? ResultadoOperacion<PacienteDto>.Ok(paciente)
                : ResultadoOperacion<PacienteDto>.Fallo("El paciente se creó pero no se pudo leer la respuesta.");
        }

        return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct));
    }

    public async Task<ResultadoOperacion<PacienteDto>> ActualizarAsync(
        Guid pacienteId,
        ActualizarPerfilRequest request,
        CancellationToken ct = default)
    {
        var respuesta = await http.PutAsJsonAsync($"api/pacientes/{pacienteId}", request, ct);

        if (respuesta.IsSuccessStatusCode)
        {
            var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteDto>(cancellationToken: ct);
            return paciente is not null
                ? ResultadoOperacion<PacienteDto>.Ok(paciente)
                : ResultadoOperacion<PacienteDto>.Fallo("El paciente se actualizó pero no se pudo leer la respuesta.");
        }

        return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct));
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return error?.Error ?? "No fue posible completar la operación.";
        }
        catch
        {
            return "No fue posible completar la operación.";
        }
    }
}
