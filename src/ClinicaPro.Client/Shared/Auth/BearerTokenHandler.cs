using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Adjunta el JWT únicamente a endpoints protegidos y centraliza la reacción
/// ante una sesión vencida. Las rutas públicas de autenticación nunca reciben
/// un Bearer residual de una sesión anterior.
/// </summary>
public sealed class BearerTokenHandler(
    TokenStorageService tokenStorage,
    ApiAuthenticationStateProvider authStateProvider,
    NavigationManager navigation) : DelegatingHandler
{
    private static readonly string[] RutasPublicasAuth =
    [
        "/api/auth/login",
        "/api/auth/register/paciente",
        "/api/auth/forgot-password",
        "/api/auth/reset-password"
    ];

    private int cierreEnCurso;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var esPublica = EsRutaPublicaDeAuth(request);
        AuthResponse? sesion = null;
        var envioBearer = false;

        if (!esPublica)
        {
            // Primero usamos la sesión compartida en memoria. Si este handler fue
            // construido antes del login, hacemos una lectura fresca del navegador.
            sesion = tokenStorage.Actual ?? await tokenStorage.ObtenerFrescoAsync();

            if (sesion is not null && !string.IsNullOrWhiteSpace(sesion.AccessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    sesion.AccessToken);
                envioBearer = true;
            }
            else
            {
                request.Headers.Authorization = null;
            }
        }
        else
        {
            request.Headers.Authorization = null;
        }

        var respuesta = await base.SendAsync(request, cancellationToken);

        // Solo tratamos un 401 como sesión vencida si REALMENTE enviamos un JWT.
        // Un 401 sin Bearer no debe destruir una sesión recién creada por otro flujo.
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
            && envioBearer
            && !esPublica)
        {
            await CerrarSesionVencidaUnaVezAsync();
        }

        return respuesta;
    }

    private static bool EsRutaPublicaDeAuth(HttpRequestMessage request)
    {
        var ruta = request.RequestUri?.AbsolutePath ?? string.Empty;
        return RutasPublicasAuth.Any(
            publica => ruta.EndsWith(publica, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CerrarSesionVencidaUnaVezAsync()
    {
        if (Interlocked.Exchange(ref cierreEnCurso, 1) == 1)
        {
            return;
        }

        try
        {
            await tokenStorage.LimpiarAsync();
            authStateProvider.NotificarSesionCerrada();
            navigation.NavigateTo("/login?sesion=vencida", forceLoad: false, replace: true);
        }
        finally
        {
            Interlocked.Exchange(ref cierreEnCurso, 0);
        }
    }
}
