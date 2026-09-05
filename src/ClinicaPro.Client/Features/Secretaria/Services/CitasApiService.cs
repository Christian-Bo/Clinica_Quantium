namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class CitasApiService(ApiClient api)
{
    private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss";

    public Task<IReadOnlyList<CitaDto>> ListarAgendaAsync(
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

        return api.ObtenerListaAsync<CitaDto>(url, "No fue posible cargar la agenda.", ct);
    }

    public Task<IReadOnlyList<CitaDto>> ListarPendientesAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<CitaDto>(
            "api/citas/pendientes",
            "No fue posible cargar las solicitudes pendientes.",
            ct);

    public Task<IReadOnlyList<CitaDto>> ListarMisCitasAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<CitaDto>(
            "api/citas/medico",
            "No fue posible cargar tu agenda médica.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> IniciarAtencionAsync(Guid citaId, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/iniciar",
            null,
            "No fue posible iniciar la atención.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> FinalizarAtencionAsync(Guid citaId, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/finalizar",
            null,
            "No fue posible finalizar la atención.",
            ct);

    public Task<IReadOnlyList<CitaDto>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken ct = default)
        => api.ObtenerListaAsync<CitaDto>(
            $"api/citas?pacienteId={pacienteId}",
            "No fue posible cargar el historial del paciente.",
            ct,
            notFoundComoVacio: true);

    public Task<ResultadoOperacion<HistorialMedicoPacienteDto>> ObtenerHistorialMedicoPacienteSeguroAsync(
        Guid pacienteId,
        CancellationToken ct = default)
        => api.ObtenerResultadoAsync<HistorialMedicoPacienteDto>(
            $"api/citas/paciente/{pacienteId}/historial-medico",
            "No fue posible cargar el historial médico permitido.",
            ct);

    public async Task<HistorialMedicoPacienteDto?> ObtenerHistorialMedicoPacienteAsync(
        Guid pacienteId,
        CancellationToken ct = default)
    {
        var resultado = await ObtenerHistorialMedicoPacienteSeguroAsync(pacienteId, ct);
        return resultado.Exito ? resultado.Valor : null;
    }

    public Task<ResultadoOperacion<AutorizacionReprogramacionDto>> SolicitarAutorizacionReprogramacionAsync(
        Guid citaId,
        string motivo,
        CancellationToken ct = default)
        => api.EnviarAsync<AutorizacionReprogramacionDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/solicitar-autorizacion-reprogramacion",
            new MotivoCitaRequest(motivo),
            "No fue posible solicitar la autorización.",
            ct);

    public Task<IReadOnlyList<SlotDisponibleDto>> ListarDisponibilidadAsync(
        DateOnly fecha,
        CancellationToken ct = default)
        => api.ObtenerListaAsync<SlotDisponibleDto>(
            $"api/citas/disponibilidad?fecha={fecha:yyyy-MM-dd}",
            "No fue posible consultar los horarios disponibles.",
            ct);

    public Task<IReadOnlyList<HistorialCitaDto>> ObtenerHistorialAsync(
        Guid citaId,
        CancellationToken ct = default)
        => api.ObtenerListaAsync<HistorialCitaDto>(
            $"api/citas/{citaId}/historial",
            "No fue posible cargar el historial de la cita.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> CrearParaPacienteAsync(
        SolicitarCitaParaPacienteRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            "api/citas/para-paciente",
            request,
            "No fue posible registrar la cita.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> ConfirmarAsync(Guid citaId, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/confirmar",
            null,
            "No fue posible confirmar la cita.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> RechazarAsync(Guid citaId, string? motivo, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/rechazar",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible rechazar la solicitud.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> ReprogramarAsync(
        Guid citaId,
        DateTime nuevaFechaHoraInicio,
        string? motivo,
        CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/reprogramar",
            new ReprogramarCitaRequest(nuevaFechaHoraInicio, motivo),
            "No fue posible reprogramar la cita.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> CancelarAdministrativaAsync(
        Guid citaId,
        string? motivo,
        CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/cancelar-administrativa",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible cancelar la cita.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> RegistrarLlegadaAsync(Guid citaId, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/llegada",
            null,
            "No fue posible registrar la llegada.",
            ct);

    public Task<ResultadoOperacion<CitaDto>> MarcarNoPresentadaAsync(Guid citaId, CancellationToken ct = default)
        => api.EnviarAsync<CitaDto>(
            HttpMethod.Post,
            $"api/citas/{citaId}/no-presentada",
            null,
            "No fue posible marcar la cita como no presentada.",
            ct);
}
