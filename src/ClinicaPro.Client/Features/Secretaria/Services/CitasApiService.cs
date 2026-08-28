namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class CitasApiService(HttpClient http)
{
    private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss";

    public async Task<IReadOnlyList<CitaDto>> ListarAgendaAsync(
        DateTime desde,
        DateTime hasta,
        Guid? medicoId = null,
        CancellationToken ct = default)
    {
        var url = $"api/citas/agenda?desde={Uri.EscapeDataString(desde.ToString(FormatoFecha))}" +
                  $"&hasta={Uri.EscapeDataString(hasta.ToString(FormatoFecha))}";

        if (medicoId is not null)
        {
            url += $"&medicoId={medicoId}";
        }

        return await http.GetFromJsonAsync<List<CitaDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<CitaDto>> ListarPendientesAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<CitaDto>>("api/citas/pendientes", ct) ?? [];

    /// <summary>Historial administrativo de citas de un paciente específico (fecha, médico, estado). Devuelve
    /// lista vacía si el paciente no existe, en vez de propagar el 404 al llamador.</summary>
    public async Task<IReadOnlyList<CitaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken ct = default)
    {
        var respuesta = await http.GetAsync($"api/citas?pacienteId={pacienteId}", ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            return [];
        }

        return await respuesta.Content.ReadFromJsonAsync<List<CitaDto>>(cancellationToken: ct) ?? [];
    }

    public async Task<IReadOnlyList<SlotDisponibleDto>> ListarDisponibilidadAsync(
        Guid especialidadId,
        DateOnly fecha,
        CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<SlotDisponibleDto>>(
            $"api/citas/disponibilidad?especialidadId={especialidadId}&fecha={fecha:yyyy-MM-dd}", ct) ?? [];

    public async Task<IReadOnlyList<HistorialCitaDto>> ObtenerHistorialAsync(
        Guid citaId,
        CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<HistorialCitaDto>>($"api/citas/{citaId}/historial", ct) ?? [];

    public Task<ResultadoOperacion<CitaDto>> CrearParaPacienteAsync(
        SolicitarCitaParaPacienteRequest request,
        CancellationToken ct = default)
        => EnviarAsync(() => http.PostAsJsonAsync("api/citas/para-paciente", request, ct), ct);

    public Task<ResultadoOperacion<CitaDto>> ConfirmarAsync(Guid citaId, CancellationToken ct = default)
        => AccionSimpleAsync($"api/citas/{citaId}/confirmar", ct);

    public Task<ResultadoOperacion<CitaDto>> RechazarAsync(Guid citaId, string? motivo, CancellationToken ct = default)
        => EnviarAsync(
            () => http.PostAsJsonAsync($"api/citas/{citaId}/rechazar", new MotivoCitaRequest(motivo ?? string.Empty), ct),
            ct);

    public Task<ResultadoOperacion<CitaDto>> ReprogramarAsync(
        Guid citaId,
        DateTime nuevaFechaHoraInicio,
        string? motivo,
        CancellationToken ct = default)
        => EnviarAsync(
            () => http.PostAsJsonAsync($"api/citas/{citaId}/reprogramar", new ReprogramarCitaRequest(nuevaFechaHoraInicio, motivo), ct),
            ct);

    public Task<ResultadoOperacion<CitaDto>> CancelarAdministrativaAsync(
        Guid citaId,
        string? motivo,
        CancellationToken ct = default)
        => EnviarAsync(
            () => http.PostAsJsonAsync($"api/citas/{citaId}/cancelar-administrativa", new MotivoCitaRequest(motivo ?? string.Empty), ct),
            ct);

    public Task<ResultadoOperacion<CitaDto>> RegistrarLlegadaAsync(Guid citaId, CancellationToken ct = default)
        => AccionSimpleAsync($"api/citas/{citaId}/llegada", ct);

    public Task<ResultadoOperacion<CitaDto>> MarcarNoPresentadaAsync(Guid citaId, CancellationToken ct = default)
        => AccionSimpleAsync($"api/citas/{citaId}/no-presentada", ct);

    private Task<ResultadoOperacion<CitaDto>> AccionSimpleAsync(string url, CancellationToken ct)
        => EnviarAsync(() => http.PostAsync(url, content: null, ct), ct);

    private static async Task<ResultadoOperacion<CitaDto>> EnviarAsync(
        Func<Task<HttpResponseMessage>> enviar,
        CancellationToken ct)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await enviar();
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<CitaDto>.Fallo("No se pudo contactar al servidor. Verifica tu conexión.");
        }

        if (respuesta.IsSuccessStatusCode)
        {
            var cita = await respuesta.Content.ReadFromJsonAsync<CitaDto>(cancellationToken: ct);
            return cita is not null
                ? ResultadoOperacion<CitaDto>.Ok(cita)
                : ResultadoOperacion<CitaDto>.Fallo("La cita se procesó pero no se pudo leer la respuesta.");
        }

        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
            return ResultadoOperacion<CitaDto>.Fallo(error?.Error ?? "No fue posible completar la acción sobre la cita.");
        }
        catch
        {
            return ResultadoOperacion<CitaDto>.Fallo("No fue posible completar la acción sobre la cita.");
        }
    }
}
