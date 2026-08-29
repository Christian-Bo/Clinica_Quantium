using ClinicaPro.Contracts.Admin;

namespace ClinicaPro.Client.Features.Admin.Services;

public sealed record EspecialidadAdminItem(
    Guid EspecialidadId,
    string Nombre,
    string? Descripcion,
    bool IsActive);

public sealed class AdminApiService(HttpClient http)
{
    public async Task<IReadOnlyList<EspecialidadAdminItem>> ListarEspecialidadesAsync(CancellationToken ct = default)
    {
        var todas = await http.GetFromJsonAsync<List<EspecialidadDto>>("api/admin/especialidades", ct) ?? [];
        var activas = await http.GetFromJsonAsync<List<EspecialidadDto>>("api/especialidades", ct) ?? [];
        var idsActivos = activas.Select(x => x.EspecialidadId).ToHashSet();

        return todas
            .Select(x => new EspecialidadAdminItem(x.EspecialidadId, x.Nombre, x.Descripcion, idsActivos.Contains(x.EspecialidadId)))
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Nombre)
            .ToList();
    }

    public Task<IReadOnlyList<MedicoDto>> ListarMedicosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<MedicoDto>("api/medicos", ct);

    public Task<IReadOnlyList<HorarioDto>> ListarHorariosAsync(Guid medicoId, CancellationToken ct = default)
        => ObtenerListaAsync<HorarioDto>($"api/medicos/{medicoId}/horarios", ct);

    public Task<IReadOnlyList<UsuarioAdminDto>> ListarUsuariosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<UsuarioAdminDto>("api/admin/usuarios", ct);

    public Task<IReadOnlyList<ParametroDto>> ListarParametrosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<ParametroDto>("api/parametros", ct);

    public Task<IReadOnlyList<AuditoriaDto>> ListarAuditoriaAsync(CancellationToken ct = default)
        => ObtenerListaAsync<AuditoriaDto>("api/admin/auditoria", ct);

    public async Task<ResultadoOperacion<EspecialidadDto>> CrearEspecialidadAsync(
        CrearEspecialidadRequest request,
        CancellationToken ct = default)
        => await EnviarConRespuestaAsync<CrearEspecialidadRequest, EspecialidadDto>(
            HttpMethod.Post, "api/admin/especialidades", request, "No fue posible crear la especialidad.", ct);

    public async Task<ResultadoOperacion<EspecialidadDto>> ActualizarEspecialidadAsync(
        Guid especialidadId,
        ActualizarEspecialidadRequest request,
        CancellationToken ct = default)
        => await EnviarConRespuestaAsync<ActualizarEspecialidadRequest, EspecialidadDto>(
            HttpMethod.Put, $"api/admin/especialidades/{especialidadId}", request, "No fue posible actualizar la especialidad.", ct);

    public async Task<ResultadoOperacion<MedicoDto>> CrearMedicoAsync(
        CrearMedicoRequest request,
        CancellationToken ct = default)
        => await EnviarConRespuestaAsync<CrearMedicoRequest, MedicoDto>(
            HttpMethod.Post, "api/admin/medicos", request, "No fue posible crear al médico.", ct);

    public async Task<ResultadoOperacion<MedicoDto>> ActualizarMedicoAsync(
        Guid medicoId,
        ActualizarMedicoRequest request,
        CancellationToken ct = default)
        => await EnviarConRespuestaAsync<ActualizarMedicoRequest, MedicoDto>(
            HttpMethod.Put, $"api/admin/medicos/{medicoId}", request, "No fue posible actualizar al médico.", ct);

    public async Task<ResultadoOperacion<HorarioDto>> CrearHorarioAsync(
        Guid medicoId,
        CrearHorarioRequest request,
        CancellationToken ct = default)
        => await EnviarConRespuestaAsync<CrearHorarioRequest, HorarioDto>(
            HttpMethod.Post, $"api/admin/medicos/{medicoId}/horarios", request, "No fue posible crear el horario.", ct);

    public async Task<ResultadoOperacion<bool>> EliminarHorarioAsync(Guid horarioId, CancellationToken ct = default)
        => await EnviarSinRespuestaAsync(
            HttpMethod.Delete, $"api/admin/horarios/{horarioId}", null, "No fue posible eliminar el horario.", ct);

    public async Task<ResultadoOperacion<bool>> ActualizarUsuarioAsync(
        Guid usuarioId,
        bool isActive,
        CancellationToken ct = default)
        => await EnviarSinRespuestaAsync(
            HttpMethod.Put,
            $"api/admin/usuarios/{usuarioId}",
            new ActualizarUsuarioRequest(isActive),
            "No fue posible actualizar el usuario.",
            ct);

    public async Task<ResultadoOperacion<bool>> ActualizarParametroAsync(
        string clave,
        string valor,
        CancellationToken ct = default)
        => await EnviarSinRespuestaAsync(
            HttpMethod.Put,
            $"api/admin/parametros/{Uri.EscapeDataString(clave)}",
            new ActualizarParametroRequest(valor),
            "No fue posible actualizar el parámetro.",
            ct);

    private async Task<IReadOnlyList<T>> ObtenerListaAsync<T>(string url, CancellationToken ct)
        => await http.GetFromJsonAsync<List<T>>(url, ct) ?? [];

    private async Task<ResultadoOperacion<TResponse>> EnviarConRespuestaAsync<TRequest, TResponse>(
        HttpMethod method,
        string url,
        TRequest request,
        string mensajeError,
        CancellationToken ct)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(request)
            };
            using var respuesta = await http.SendAsync(mensaje, ct);

            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<TResponse>.Fallo(await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct));
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<TResponse>(cancellationToken: ct);
            return valor is null
                ? ResultadoOperacion<TResponse>.Fallo(mensajeError)
                : ResultadoOperacion<TResponse>.Ok(valor);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<TResponse>.Fallo("No se pudo contactar a la API.");
        }
    }

    private async Task<ResultadoOperacion<bool>> EnviarSinRespuestaAsync(
        HttpMethod method,
        string url,
        object? request,
        string mensajeError,
        CancellationToken ct)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(method, url);
            if (request is not null)
            {
                mensaje.Content = JsonContent.Create(request);
            }

            using var respuesta = await http.SendAsync(mensaje, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<bool>.Fallo(await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct));
            }

            return ResultadoOperacion<bool>.Ok(true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo("No se pudo contactar a la API.");
        }
    }
}
