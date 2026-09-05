using System.Net;

namespace ClinicaPro.Client.Shared.Auth;

public sealed class AuthApiService(
    HttpClient http,
    TokenStorageService tokenStorage,
    ApiAuthenticationStateProvider authStateProvider)
{
    public async Task<ResultadoOperacion<AuthResponse>> IniciarSesionAsync(
        string email,
        string password,
        bool recordarme = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(email.Trim(), password),
                cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                var mensaje = respuesta.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Correo o contraseña incorrectos.",
                    (HttpStatusCode)423 => "La cuenta está bloqueada temporalmente por seguridad.",
                    (HttpStatusCode)429 => "Demasiados intentos. Espera unos minutos antes de volver a ingresar.",
                    _ => await ApiErrorReader.LeerAsync(
                        respuesta,
                        "No fue posible iniciar sesión.",
                        cancellationToken)
                };

                return ResultadoOperacion<AuthResponse>.Fallo(mensaje, respuesta.StatusCode);
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(
                cancellationToken: cancellationToken);

            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo("El servidor no devolvió una sesión válida.")
                : await GuardarSesionAsync(sesion, recordarme);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                ex is TaskCanceledException
                    ? "El servidor tardó demasiado en responder. Intenta nuevamente."
                    : "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task<ResultadoOperacion<AuthResponse>> RegistrarPacienteAsync(
        RegisterPacienteRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.PostAsJsonAsync(
                "api/auth/register/paciente", request, cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<AuthResponse>.Fallo(
                    await ApiErrorReader.LeerAsync(
                        respuesta,
                        "No fue posible crear la cuenta.",
                        cancellationToken),
                    respuesta.StatusCode);
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(
                cancellationToken: cancellationToken);

            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo(
                    "La cuenta se creó pero no se pudo iniciar sesión. Ingresa desde la pantalla de acceso.")
                : await GuardarSesionAsync(sesion, persistente: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task<ResultadoOperacion<AuthResponse>> CambiarContrasenaAsync(
        string actual,
        string nueva,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.PostAsJsonAsync(
                "api/auth/change-password",
                new ChangePasswordRequest(actual, nueva),
                cancellationToken);

            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<AuthResponse>.Fallo(
                    await ApiErrorReader.LeerAsync(
                        respuesta,
                        "No fue posible cambiar la contraseña.",
                        cancellationToken),
                    respuesta.StatusCode);
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(
                cancellationToken: cancellationToken);

            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo(
                    "La contraseña cambió, pero no se pudo actualizar la sesión.")
                : await GuardarSesionAsync(sesion, persistente: false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo("No se pudo contactar al servidor.");
        }
    }

    public async Task<ResultadoOperacion<bool>> SolicitarCodigoRecuperacionAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.PostAsJsonAsync(
                "api/auth/forgot-password",
                new ForgotPasswordRequest(email.Trim()),
                cancellationToken);

            return respuesta.IsSuccessStatusCode
                ? ResultadoOperacion<bool>.Ok(true)
                : ResultadoOperacion<bool>.Fallo(
                    await ApiErrorReader.LeerAsync(
                        respuesta,
                        "No fue posible enviar el código.",
                        cancellationToken),
                    respuesta.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task<ResultadoOperacion<bool>> RestablecerPasswordAsync(
        string email,
        string codigo,
        string passwordNueva,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.PostAsJsonAsync(
                "api/auth/reset-password",
                new ResetPasswordRequest(email.Trim(), codigo.Trim(), passwordNueva),
                cancellationToken);

            return respuesta.IsSuccessStatusCode
                ? ResultadoOperacion<bool>.Ok(true)
                : ResultadoOperacion<bool>.Fallo(
                    await ApiErrorReader.LeerAsync(
                        respuesta,
                        "El código no es válido o ya venció.",
                        cancellationToken),
                    respuesta.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task<ResultadoOperacion<UsuarioActualDto>> ObtenerUsuarioActualSeguroAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await http.GetAsync("api/auth/me", cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<UsuarioActualDto>.Fallo(
                    await ApiErrorReader.LeerAsync(
                        respuesta,
                        "No fue posible validar la sesión.",
                        cancellationToken),
                    respuesta.StatusCode);
            }

            var usuario = await respuesta.Content.ReadFromJsonAsync<UsuarioActualDto>(
                cancellationToken: cancellationToken);

            return usuario is null
                ? ResultadoOperacion<UsuarioActualDto>.Fallo("La sesión no devolvió un usuario válido.")
                : ResultadoOperacion<UsuarioActualDto>.Ok(usuario);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<UsuarioActualDto>.Fallo(
                "No se pudo validar la sesión porque no hay conexión con el servidor.");
        }
    }

    public async Task<UsuarioActualDto?> ObtenerUsuarioActualAsync(
        CancellationToken cancellationToken = default)
    {
        var resultado = await ObtenerUsuarioActualSeguroAsync(cancellationToken);
        return resultado.Exito ? resultado.Valor : null;
    }

    public async Task CerrarSesionAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();
    }

    private async Task<ResultadoOperacion<AuthResponse>> GuardarSesionAsync(
        AuthResponse sesion,
        bool persistente = false)
    {
        await tokenStorage.GuardarAsync(sesion, persistente);
        authStateProvider.NotificarSesionIniciada(sesion);
        return ResultadoOperacion<AuthResponse>.Ok(sesion);
    }
}
