namespace ClinicaPro.Client.Features.Paciente.Services;

/// <summary>
/// Citas vistas desde el portal del paciente. Solo expone las acciones que la
/// API permite al rol Paciente: solicitar, consultar las propias, confirmar
/// asistencia, anular una solicitud y cancelar una cita ya programada.
/// </summary>
public sealed class CitasPacienteApiService(HttpClient http)
{
    /// <summary>
    /// La API interpreta FechaHoraInicio como hora de Guatemala, sin sufijo Z.
    /// Por eso se serializa sin zona horaria.
    /// </summary>
    private const string FormatoFechaHora = "yyyy-MM-ddTHH:mm:ss";

    /// <summary>
    /// Las citas del paciente autenticado. Devolver una lista vacía cuando la
    /// API falla haría que un error de red se vea igual que "no tienes citas",
    /// y el paciente creería que su solicitud nunca se registró.
    /// </summary>
    public async Task<ResultadoOperacion<IReadOnlyList<CitaDto>>> ListarMisCitasAsync(
        CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync("api/citas/mias", ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<IReadOnlyList<CitaDto>>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return ResultadoOperacion<IReadOnlyList<CitaDto>>.Fallo(
                await ApiErrorReader.LeerAsync(respuesta, "No pudimos leer tus citas.", ct));
        }

        var citas = await respuesta.Content.ReadFromJsonAsync<List<CitaDto>>(cancellationToken: ct);
        return ResultadoOperacion<IReadOnlyList<CitaDto>>.Ok(citas ?? []);
    }

    /// <summary>
    /// Devuelve todos los espacios libres de los médicos activos para una fecha.
    /// El backend ya entrega el médico asociado a cada slot, por lo que el cliente
    /// no necesita resolver especialidades ni asociaciones intermedias.
    /// </summary>
    public async Task<ResultadoOperacion<IReadOnlyList<SlotDisponibleDto>>> ListarDisponibilidadAsync(
        DateOnly fecha,
        CancellationToken ct = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.GetAsync($"api/citas/disponibilidad?fecha={fecha:yyyy-MM-dd}", ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<IReadOnlyList<SlotDisponibleDto>>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión.");
        }

        if (respuesta.IsSuccessStatusCode)
        {
            var slots = await respuesta.Content.ReadFromJsonAsync<List<SlotDisponibleDto>>(cancellationToken: ct);
            return ResultadoOperacion<IReadOnlyList<SlotDisponibleDto>>.Ok(slots ?? []);
        }

        return ResultadoOperacion<IReadOnlyList<SlotDisponibleDto>>.Fallo(
            await ApiErrorReader.LeerAsync(respuesta, "No fue posible consultar los horarios disponibles.", ct));
    }

    public Task<ResultadoOperacion<CitaDto>> SolicitarAsync(
        Guid medicoId,
        DateTime fechaHoraInicio,
        string motivoConsulta,
        CancellationToken ct = default)
        => EnviarAsync(
            () => http.PostAsJsonAsync(
                "api/citas",
                new SolicitarCitaRequest(medicoId, fechaHoraInicio, motivoConsulta),
                ct),
            ct);

    public Task<ResultadoOperacion<CitaDto>> ConfirmarAsistenciaAsync(Guid citaId, CancellationToken ct = default)
        => EnviarAsync(() => http.PostAsync($"api/citas/{citaId}/confirmar-asistencia", content: null, ct), ct);

    public Task<ResultadoOperacion<CitaDto>> AnularSolicitudAsync(Guid citaId, CancellationToken ct = default)
        => EnviarAsync(() => http.PostAsync($"api/citas/{citaId}/anular-solicitud", content: null, ct), ct);

    public Task<ResultadoOperacion<CitaDto>> CancelarAsync(
        Guid citaId,
        string? motivo,
        CancellationToken ct = default)
        => EnviarAsync(
            () => http.PostAsJsonAsync(
                $"api/citas/{citaId}/cancelar",
                new MotivoCitaRequest(motivo ?? string.Empty),
                ct),
            ct);

    public static string FormatearParaApi(DateTime fechaHora) => fechaHora.ToString(FormatoFechaHora);

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
            return ResultadoOperacion<CitaDto>.Fallo(
                error?.Error ?? "No fue posible completar la acción sobre la cita.");
        }
        catch
        {
            return ResultadoOperacion<CitaDto>.Fallo("No fue posible completar la acción sobre la cita.");
        }
    }
}
