namespace ClinicaPro.Client.Shared.Auth;

public sealed class AuthApiService(
    HttpClient http,
    TokenStorageService tokenStorage,
    ApiAuthenticationStateProvider authStateProvider)
{
    public async Task<ResultadoOperacion<AuthResponse>> IniciarSesionAsync(
        string email,
        string password,
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
                : await GuardarSesionAsync(sesion);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica que la API esté corriendo y vuelve a intentar.");
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

    private async Task<ResultadoOperacion<AuthResponse>> GuardarSesionAsync(AuthResponse sesion)
    {
        await tokenStorage.GuardarAsync(sesion);
        authStateProvider.NotificarSesionIniciada(sesion);
        return ResultadoOperacion<AuthResponse>.Ok(sesion);
    }
}
