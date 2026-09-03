namespace ClinicaPro.Client.Features.Paciente.Services;

/// <summary>
/// Datos del propio paciente autenticado. Solo usa los endpoints "me":
/// el paciente nunca consulta ni edita fichas ajenas.
/// </summary>
public sealed class PerfilPacienteApiService(HttpClient http)
{
    public async Task<ResultadoOperacion<PacienteDto>> ObtenerMiFichaAsync(CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync("api/pacientes/me", ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<PacienteDto>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }

        if (respuesta.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return ResultadoOperacion<PacienteDto>.Fallo(
                "Tu usuario todavía no tiene una ficha de paciente. Comunícate con la clínica.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct));
        }

        var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteDto>(cancellationToken: ct);
        return paciente is not null
            ? ResultadoOperacion<PacienteDto>.Ok(paciente)
            : ResultadoOperacion<PacienteDto>.Fallo("No se pudo leer tu información.");
    }

    public async Task<ResultadoOperacion<PacienteDto>> ActualizarMiFichaAsync(
        ActualizarPerfilRequest request,
        CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PutAsJsonAsync("api/pacientes/me", request, ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<PacienteDto>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct));
        }

        var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteDto>(cancellationToken: ct);
        return paciente is not null
            ? ResultadoOperacion<PacienteDto>.Ok(paciente)
            : ResultadoOperacion<PacienteDto>.Fallo("Los cambios se guardaron pero no se pudo leer la respuesta.");
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
