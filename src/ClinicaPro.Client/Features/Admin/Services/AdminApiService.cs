using ClinicaPro.Contracts.Admin;
using ClinicaPro.Client.Features.Secretaria.Services;

namespace ClinicaPro.Client.Features.Admin.Services;

public sealed class AdminApiService(ApiClient api, MedicosCacheService medicosCache)
{
    public Task<IReadOnlyList<AdminMedicoDto>> ListarMedicosAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<AdminMedicoDto>(
            "api/admin/medicos",
            "No fue posible cargar los médicos.",
            ct);

    public Task<IReadOnlyList<HorarioDto>> ListarHorariosAsync(Guid medicoId, CancellationToken ct = default)
        => api.ObtenerListaAsync<HorarioDto>(
            $"api/medicos/{medicoId}/horarios",
            "No fue posible cargar los horarios del médico.",
            ct);

    public Task<IReadOnlyList<UsuarioAdminDto>> ListarUsuariosAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<UsuarioAdminDto>(
            "api/admin/usuarios",
            "No fue posible cargar los usuarios.",
            ct);

    public Task<IReadOnlyList<ParametroDto>> ListarParametrosAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<ParametroDto>(
            "api/parametros",
            "No fue posible cargar los parámetros.",
            ct);

    public Task<IReadOnlyList<AuditoriaDto>> ListarAuditoriaAsync(CancellationToken ct = default)
        => api.ObtenerListaAsync<AuditoriaDto>(
            "api/admin/auditoria",
            "No fue posible cargar la auditoría.",
            ct);

    public Task<IReadOnlyList<AutorizacionReprogramacionDto>> ListarAutorizacionesAsync(
        string? estado = null,
        CancellationToken ct = default)
    {
        var url = "api/admin/autorizaciones-reprogramacion";
        if (!string.IsNullOrWhiteSpace(estado))
        {
            url += $"?estado={Uri.EscapeDataString(estado)}";
        }

        return api.ObtenerListaAsync<AutorizacionReprogramacionDto>(
            url,
            "No fue posible cargar las autorizaciones.",
            ct);
    }

    public async Task<ResultadoOperacion<MedicoDto>> CrearMedicoAsync(
        CrearMedicoRequest request,
        CancellationToken ct = default)
    {
        var resultado = await api.EnviarAsync<MedicoDto>(
            HttpMethod.Post,
            "api/admin/medicos",
            request,
            "No fue posible crear al médico.",
            ct);

        if (resultado.Exito)
        {
            medicosCache.Invalidar();
        }

        return resultado;
    }

    public async Task<ResultadoOperacion<MedicoDto>> ActualizarMedicoAsync(
        Guid medicoId,
        ActualizarMedicoRequest request,
        CancellationToken ct = default)
    {
        var resultado = await api.EnviarAsync<MedicoDto>(
            HttpMethod.Put,
            $"api/admin/medicos/{medicoId}",
            request,
            "No fue posible actualizar al médico.",
            ct);

        if (resultado.Exito)
        {
            medicosCache.Invalidar();
        }

        return resultado;
    }

    public Task<ResultadoOperacion<HorarioDto>> CrearHorarioAsync(
        Guid medicoId,
        CrearHorarioRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<HorarioDto>(
            HttpMethod.Post,
            $"api/admin/medicos/{medicoId}/horarios",
            request,
            "No fue posible crear el horario.",
            ct);

    public Task<ResultadoOperacion<HorarioDto>> ActualizarHorarioAsync(
        Guid medicoId,
        Guid horarioId,
        ActualizarHorarioRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<HorarioDto>(
            HttpMethod.Put,
            $"api/admin/medicos/{medicoId}/horarios/{horarioId}",
            request,
            "No fue posible actualizar el horario.",
            ct);

    public Task<ResultadoOperacion<bool>> EliminarHorarioAsync(Guid horarioId, CancellationToken ct = default)
        => api.EnviarSinContenidoAsync(
            HttpMethod.Delete,
            $"api/admin/horarios/{horarioId}",
            null,
            "No fue posible eliminar el horario.",
            ct);

    public Task<ResultadoOperacion<UsuarioAdminDto>> CrearUsuarioAsync(
        CrearUsuarioStaffRequest request,
        CancellationToken ct = default)
        => api.EnviarAsync<UsuarioAdminDto>(
            HttpMethod.Post,
            "api/admin/usuarios",
            request,
            "No fue posible crear el usuario.",
            ct);

    public Task<ResultadoOperacion<UsuarioAdminDto>> ActualizarRolesAsync(
        Guid usuarioId,
        IReadOnlyList<string> roles,
        CancellationToken ct = default)
        => api.EnviarAsync<UsuarioAdminDto>(
            HttpMethod.Put,
            $"api/admin/usuarios/{usuarioId}/roles",
            new ActualizarRolesUsuarioRequest(roles),
            "No fue posible actualizar los roles.",
            ct);

    public Task<ResultadoOperacion<bool>> ActualizarUsuarioAsync(
        Guid usuarioId,
        bool isActive,
        CancellationToken ct = default)
        => api.EnviarSinContenidoAsync(
            HttpMethod.Put,
            $"api/admin/usuarios/{usuarioId}",
            new ActualizarUsuarioRequest(isActive),
            "No fue posible actualizar el usuario.",
            ct);

    public Task<ResultadoOperacion<AutorizacionReprogramacionDto>> AprobarAutorizacionAsync(
        Guid autorizacionId,
        string? motivo,
        CancellationToken ct = default)
        => api.EnviarAsync<AutorizacionReprogramacionDto>(
            HttpMethod.Post,
            $"api/admin/autorizaciones-reprogramacion/{autorizacionId}/aprobar",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible aprobar la autorización.",
            ct);

    public Task<ResultadoOperacion<AutorizacionReprogramacionDto>> RechazarAutorizacionAsync(
        Guid autorizacionId,
        string? motivo,
        CancellationToken ct = default)
        => api.EnviarAsync<AutorizacionReprogramacionDto>(
            HttpMethod.Post,
            $"api/admin/autorizaciones-reprogramacion/{autorizacionId}/rechazar",
            new MotivoCitaRequest(motivo ?? string.Empty),
            "No fue posible rechazar la autorización.",
            ct);

    public Task<ResultadoOperacion<bool>> ActualizarParametroAsync(
        string clave,
        string valor,
        CancellationToken ct = default)
        => api.EnviarSinContenidoAsync(
            HttpMethod.Put,
            $"api/admin/parametros/{Uri.EscapeDataString(clave)}",
            new ActualizarParametroRequest(valor),
            "No fue posible actualizar el parámetro.",
            ct);
}
