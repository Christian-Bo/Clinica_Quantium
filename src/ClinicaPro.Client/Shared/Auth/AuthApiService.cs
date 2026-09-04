namespace ClinicaPro.Client.Shared.Auth;

public sealed class AuthApiService(
    HttpClient http,
    TokenStorageService tokenStorage,
    ApiAuthenticationStateProvider authStateProvider)
{
    /// <param name="recordarme">
    /// false guarda la sesión solo en la pestaña actual: muere al cerrarla.
    /// Es lo que pide el usuario cuando desmarca "mantener sesión iniciada".
    /// </param>
    public async Task<ResultadoOperacion<AuthResponse>> IniciarSesionAsync(
        string email,
        string password,
        bool recordarme = true,
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
                return ResultadoOperacion<AuthResponse>.Fallo(
                    await ApiErrorReader.LeerAsync(respuesta, "Correo o contraseña incorrectos.", cancellationToken));
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo("El servidor no devolvió una sesión válida.")
                : await GuardarSesionAsync(sesion, recordarme);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica que la API esté corriendo y vuelve a intentar.");
        }
    }

    /// <summary>
    /// Registro público de un paciente. La API devuelve la sesión ya iniciada,
    /// así que entra directo a su portal sin volver a escribir la contraseña.
    /// </summary>
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
                    await ApiErrorReader.LeerAsync(respuesta, "No fue posible crear la cuenta.", cancellationToken));
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo(
                    "La cuenta se creó pero no se pudo iniciar sesión. Ingresa desde la pantalla de acceso.")
                : await GuardarSesionAsync(sesion);
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
                    await ApiErrorReader.LeerAsync(respuesta, "No fue posible cambiar la contraseña.", cancellationToken));
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            return sesion is null
                ? ResultadoOperacion<AuthResponse>.Fallo("La contraseña cambió, pero no se pudo actualizar la sesión.")
                : await GuardarSesionAsync(sesion);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo("No se pudo contactar al servidor.");
        }
    }

    /// <summary>
    /// Pide a la API que envíe por correo un código para restablecer la
    /// contraseña. La API responde 200 aunque el correo no exista: es a
    /// propósito, para que nadie pueda averiguar qué correos están registrados.
    /// </summary>
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
                    await ApiErrorReader.LeerAsync(respuesta, "No fue posible enviar el código.", cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    /// <summary>Cambia la contraseña usando el código que llegó por correo.</summary>
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
                    await ApiErrorReader.LeerAsync(respuesta, "El código no es válido o ya venció.", cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task<UsuarioActualDto?> ObtenerUsuarioActualAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await http.GetFromJsonAsync<UsuarioActualDto>("api/auth/me", cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    public async Task CerrarSesionAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();
    }

    private async Task<ResultadoOperacion<AuthResponse>> GuardarSesionAsync(
        AuthResponse sesion,
        bool persistente = true)
    {
        await tokenStorage.GuardarAsync(sesion, persistente);
        authStateProvider.NotificarSesionIniciada(sesion);
        return ResultadoOperacion<AuthResponse>.Ok(sesion);
    }
}
