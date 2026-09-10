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
            return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct), respuesta.StatusCode);
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
            return ResultadoOperacion<PacienteDto>.Fallo(await LeerErrorAsync(respuesta, ct), respuesta.StatusCode);
        }

        var paciente = await respuesta.Content.ReadFromJsonAsync<PacienteDto>(cancellationToken: ct);
        return paciente is not null
            ? ResultadoOperacion<PacienteDto>.Ok(paciente)
            : ResultadoOperacion<PacienteDto>.Fallo("Los cambios se guardaron pero no se pudo leer la respuesta.");
    }

    /// <summary>
    /// Usa el lector compartido, que además de ErrorResponse entiende
    /// ValidationProblemDetails y ProblemDetails. El lector que había aquí solo
    /// miraba "error", así que un 500 del servidor caía en el mensaje genérico
    /// y escondía lo que la API realmente estaba diciendo.
    /// </summary>
    private static Task<string> LeerErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
        => ApiErrorReader.LeerAsync(respuesta, "No fue posible completar la operación.", ct);
}
