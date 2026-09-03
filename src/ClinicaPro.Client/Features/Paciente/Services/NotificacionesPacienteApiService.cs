namespace ClinicaPro.Client.Features.Paciente.Services;

/// <summary>
/// Avisos que el sistema le generó a este paciente (cita programada,
/// confirmada, reprogramada o cancelada).
/// </summary>
public sealed class NotificacionesPacienteApiService(HttpClient http)
{
    public async Task<IReadOnlyList<NotificacionDto>> ListarMisNotificacionesAsync(CancellationToken ct = default)
    {
        try
        {
            return await http.GetFromJsonAsync<List<NotificacionDto>>("api/notificaciones/mias", ct) ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}
