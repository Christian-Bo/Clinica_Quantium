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

    public Task<IReadOnlyList<AdminMedicoDto>> ListarMedicosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<AdminMedicoDto>("api/admin/medicos", ct);

    public Task<IReadOnlyList<HorarioDto>> ListarHorariosAsync(Guid medicoId, CancellationToken ct = default)
        => ObtenerListaAsync<HorarioDto>($"api/medicos/{medicoId}/horarios", ct);

    public Task<IReadOnlyList<MedicoEspecialidadAdminDto>> ListarEspecialidadesMedicoAsync(
        Guid medicoId,
        CancellationToken ct = default)
        => ObtenerListaAsync<MedicoEspecialidadAdminDto>($"api/admin/medicos/{medicoId}/especialidades", ct);

    public Task<IReadOnlyList<UsuarioAdminDto>> ListarUsuariosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<UsuarioAdminDto>("api/admin/usuarios", ct);

    public Task<IReadOnlyList<ParametroDto>> ListarParametrosAsync(CancellationToken ct = default)
        => ObtenerListaAsync<ParametroDto>("api/parametros", ct);

    public Task<IReadOnlyList<AuditoriaDto>> ListarAuditoriaAsync(CancellationToken ct = default)
        => ObtenerListaAsync<AuditoriaDto>("api/admin/auditoria", ct);

    public Task<IReadOnlyList<AutorizacionReprogramacionDto>> ListarAutorizacionesAsync(
        string? estado = null,
        CancellationToken ct = default)
    {
        var url = "api/admin/autorizaciones-reprogramacion";
        if (!string.IsNullOrWhiteSpace(estado))
        {
            url += $"?estado={Uri.EscapeDataString(estado)}";
        }

        return ObtenerListaAsync<AutorizacionReprogramacionDto>(url, ct);
    }

    public Task<ResultadoOperacion<EspecialidadDto>> CrearEspecialidadAsync(
        CrearEspecialidadRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<CrearEspecialidadRequest, EspecialidadDto>(
            HttpMethod.Post, "api/admin/especialidades", request, "No fue posible crear la especialidad.", ct);

    public Task<ResultadoOperacion<EspecialidadDto>> ActualizarEspecialidadAsync(
        Guid especialidadId,
        ActualizarEspecialidadRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<ActualizarEspecialidadRequest, EspecialidadDto>(
            HttpMethod.Put, $"api/admin/especialidades/{especialidadId}", request, "No fue posible actualizar la especialidad.", ct);

    public Task<ResultadoOperacion<MedicoDto>> CrearMedicoAsync(
        CrearMedicoRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<CrearMedicoRequest, MedicoDto>(
            HttpMethod.Post, "api/admin/medicos", request, "No fue posible crear al médico.", ct);

    public Task<ResultadoOperacion<MedicoDto>> ActualizarMedicoAsync(
        Guid medicoId,
        ActualizarMedicoRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<ActualizarMedicoRequest, MedicoDto>(
            HttpMethod.Put, $"api/admin/medicos/{medicoId}", request, "No fue posible actualizar al médico.", ct);

    public Task<ResultadoOperacion<MedicoEspecialidadAdminDto>> AgregarEspecialidadMedicoAsync(
        Guid medicoId,
        AsignarEspecialidadMedicoRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<AsignarEspecialidadMedicoRequest, MedicoEspecialidadAdminDto>(
            HttpMethod.Post,
            $"api/admin/medicos/{medicoId}/especialidades",
            request,
            "No fue posible asignar la especialidad.",
            ct);

    public Task<ResultadoOperacion<MedicoEspecialidadAdminDto>> ActualizarEspecialidadMedicoAsync(
        Guid medicoId,
        Guid especialidadId,
        ActualizarEspecialidadMedicoRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<ActualizarEspecialidadMedicoRequest, MedicoEspecialidadAdminDto>(
            HttpMethod.Put,
            $"api/admin/medicos/{medicoId}/especialidades/{especialidadId}",
            request,
            "No fue posible actualizar la especialidad del médico.",
            ct);

    public Task<ResultadoOperacion<bool>> QuitarEspecialidadMedicoAsync(
        Guid medicoId,
        Guid especialidadId,
        CancellationToken ct = default)
        => EnviarSinRespuestaAsync(
            HttpMethod.Delete,
            $"api/admin/medicos/{medicoId}/especialidades/{especialidadId}",
            null,
            "No fue posible quitar la especialidad.",
            ct);

    public Task<ResultadoOperacion<HorarioDto>> CrearHorarioAsync(
        Guid medicoId,
        CrearHorarioRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<CrearHorarioRequest, HorarioDto>(
            HttpMethod.Post, $"api/admin/medicos/{medicoId}/horarios", request, "No fue posible crear el horario.", ct);

    public Task<ResultadoOperacion<HorarioDto>> ActualizarHorarioAsync(
        Guid medicoId,
        Guid horarioId,
        ActualizarHorarioRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<ActualizarHorarioRequest, HorarioDto>(
            HttpMethod.Put,
            $"api/admin/medicos/{medicoId}/horarios/{horarioId}",
            request,
            "No fue posible actualizar el horario.",
            ct);

    public Task<ResultadoOperacion<bool>> EliminarHorarioAsync(Guid horarioId, CancellationToken ct = default)
        => EnviarSinRespuestaAsync(
            HttpMethod.Delete, $"api/admin/horarios/{horarioId}", null, "No fue posible eliminar el horario.", ct);

    public Task<ResultadoOperacion<UsuarioAdminDto>> CrearUsuarioAsync(
        CrearUsuarioStaffRequest request,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<CrearUsuarioStaffRequest, UsuarioAdminDto>(
            HttpMethod.Post, "api/admin/usuarios", request, "No fue posible crear el usuario.", ct);

    public Task<ResultadoOperacion<UsuarioAdminDto>> ActualizarRolesAsync(
        Guid usuarioId,
        IReadOnlyList<string> roles,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<ActualizarRolesUsuarioRequest, UsuarioAdminDto>(
            HttpMethod.Put,
            $"api/admin/usuarios/{usuarioId}/roles",
            new ActualizarRolesUsuarioRequest(roles),
            "No fue posible actualizar los roles.",
            ct);

    public Task<ResultadoOperacion<bool>> ActualizarUsuarioAsync(
        Guid usuarioId,
        bool isActive,
        CancellationToken ct = default)
        => EnviarSinRespuestaAsync(
            HttpMethod.Put,
            $"api/admin/usuarios/{usuarioId}",
            new ActualizarUsuarioRequest(isActive),
            "No fue posible actualizar el usuario.",
            ct);

    public Task<ResultadoOperacion<AutorizacionReprogramacionDto>> AprobarAutorizacionAsync(
        Guid autorizacionId,
        string? motivo,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<MotivoCitaRequest, AutorizacionReprogramacionDto>(
            HttpMethod.Post,
            $"api/admin/autorizaciones-reprogramacion/{autorizacionId}/aprobar",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible aprobar la autorización.",
            ct);

    public Task<ResultadoOperacion<AutorizacionReprogramacionDto>> RechazarAutorizacionAsync(
        Guid autorizacionId,
        string? motivo,
        CancellationToken ct = default)
        => EnviarConRespuestaAsync<MotivoCitaRequest, AutorizacionReprogramacionDto>(
            HttpMethod.Post,
            $"api/admin/autorizaciones-reprogramacion/{autorizacionId}/rechazar",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible rechazar la autorización.",
            ct);

    public Task<ResultadoOperacion<bool>> ActualizarParametroAsync(
        string clave,
        string valor,
        CancellationToken ct = default)
        => EnviarSinRespuestaAsync(
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
