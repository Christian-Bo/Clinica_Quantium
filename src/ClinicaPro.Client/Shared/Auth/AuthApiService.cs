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
        bool recordarme = true,
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
                await tokenStorage.GuardarAsync(sesion, recordarme);
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

    /// <summary>
    /// Registro público de un paciente. La API devuelve la sesión ya iniciada,
    /// así que el paciente entra directo a su portal sin volver a escribir la
    /// contraseña.
    /// </summary>
    public async Task<ResultadoOperacion<AuthResponse>> RegistrarPacienteAsync(
        RegisterPacienteRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PostAsJsonAsync("api/auth/register/paciente", request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
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

            return ResultadoOperacion<AuthResponse>.Fallo(
                "La cuenta se creó pero no se pudo iniciar sesión. Ingresa desde la pantalla de acceso.");
        }

        return ResultadoOperacion<AuthResponse>.Fallo(
            await LeerErrorAsync(respuesta, "No fue posible crear la cuenta.", cancellationToken));
    }

    /// <summary>
    /// Cambio de contraseña del usuario autenticado. La API devuelve una sesión
    /// nueva, que reemplaza a la actual.
    /// </summary>
    public async Task<ResultadoOperacion<AuthResponse>> CambiarPasswordAsync(
        string passwordActual,
        string passwordNueva,
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PostAsJsonAsync(
                "api/auth/change-password",
                new ChangePasswordRequest(passwordActual, passwordNueva),
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<AuthResponse>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
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

        return ResultadoOperacion<AuthResponse>.Fallo(
            await LeerErrorAsync(respuesta, "No fue posible cambiar la contraseña.", cancellationToken));
    }

    /// <summary>
    /// Pide a la API que envíe por correo un código para restablecer la
    /// contraseña. La API responde 200 aunque el correo no exista: es a
    /// propósito, para que nadie pueda averiguar qué correos están registrados.
    /// Por eso el mensaje que se muestra al usuario es siempre el mismo.
    /// </summary>
    public async Task<ResultadoOperacion<bool>> SolicitarCodigoRecuperacionAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync(
                "api/auth/forgot-password",
                new ForgotPasswordRequest(email.Trim()),
                cancellationToken);

            return respuesta.IsSuccessStatusCode
                ? ResultadoOperacion<bool>.Ok(true)
                : ResultadoOperacion<bool>.Fallo(
                    await LeerErrorAsync(respuesta, "No fue posible enviar el código.", cancellationToken));
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    /// <summary>
    /// Cambia la contraseña usando el código que llegó por correo.
    /// </summary>
    public async Task<ResultadoOperacion<bool>> RestablecerPasswordAsync(
        string email,
        string codigo,
        string passwordNueva,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var respuesta = await http.PostAsJsonAsync(
                "api/auth/reset-password",
                new ResetPasswordRequest(email.Trim(), codigo.Trim(), passwordNueva),
                cancellationToken);

            return respuesta.IsSuccessStatusCode
                ? ResultadoOperacion<bool>.Ok(true)
                : ResultadoOperacion<bool>.Fallo(
                    await LeerErrorAsync(respuesta, "El código no es válido o ya venció.", cancellationToken));
        }
        catch (HttpRequestException)
        {
            return ResultadoOperacion<bool>.Fallo(
                "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.");
        }
    }

    public async Task CerrarSesionAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();
    }

    private static async Task<string> LeerErrorAsync(
        HttpResponseMessage respuesta,
        string mensajePorDefecto,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            return string.IsNullOrWhiteSpace(error?.Error) ? mensajePorDefecto : error.Error;
        }
        catch
        {
            return mensajePorDefecto;
        }
    }
}
