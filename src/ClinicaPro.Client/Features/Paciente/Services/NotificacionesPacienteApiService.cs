namespace ClinicaPro.Client.Features.Paciente.Services;

/// <summary>
/// Avisos que el sistema le generó a este paciente (cita programada,
/// confirmada, reprogramada o cancelada).
/// </summary>
public sealed class NotificacionesPacienteApiService(HttpClient http)
{
    /// <summary>
    /// Mismo criterio que en las citas: una lista vacía significa "no hay
    /// avisos", nunca "la consulta falló". Confundirlos ocultaría avisos que
    /// el paciente sí tiene.
    /// </summary>
    public async Task<ResultadoOperacion<IReadOnlyList<NotificacionDto>>> ListarMisNotificacionesAsync(
        CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync("api/notificaciones/mias", ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<IReadOnlyList<NotificacionDto>>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return ResultadoOperacion<IReadOnlyList<NotificacionDto>>.Fallo(
                await ApiErrorReader.LeerAsync(respuesta, "No pudimos leer tus avisos.", ct));
        }

        var avisos = await respuesta.Content.ReadFromJsonAsync<List<NotificacionDto>>(cancellationToken: ct);
        return ResultadoOperacion<IReadOnlyList<NotificacionDto>>.Ok(avisos ?? []);
    }
}
