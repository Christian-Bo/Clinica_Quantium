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
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PostAsJsonAsync(
                "api/auth/login",
                new LoginRequest(email, password),
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica que la API esté corriendo y vuelve a intentar.");
        }

        if (respuesta.IsSuccessStatusCode)
        {
            var sesion = await respuesta.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
            if (sesion is not null)
            {
                await tokenStorage.GuardarAsync(sesion);
                authStateProvider.NotificarSesionIniciada(sesion);
                return ResultadoOperacion<AuthResponse>.Ok(sesion);
            }
        }

        var mensaje = respuesta.StatusCode switch
        {
            HttpStatusCode.Locked =>
                "La cuenta está bloqueada temporalmente por varios intentos fallidos. Intenta de nuevo en unos minutos.",
            HttpStatusCode.Unauthorized => "Correo o contraseña incorrectos.",
            _ => "No fue posible iniciar sesión. Intenta de nuevo."
        };

        return ResultadoOperacion<AuthResponse>.Fallo(mensaje);
    }

    public async Task CerrarSesionAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();
    }
}
