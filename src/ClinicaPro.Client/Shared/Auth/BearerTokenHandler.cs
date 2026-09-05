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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var esPublica = EsRutaPublicaDeAuth(request);
        var sesion = await tokenStorage.ObtenerAsync();

        if (!esPublica && sesion is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        }
        else
        {
            request.Headers.Authorization = null;
        }

        var respuesta = await base.SendAsync(request, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
            && sesion is not null
            && !esPublica)
        {
            await CerrarSesionVencidaAsync();
        }

        return respuesta;
    }

    private static bool EsRutaPublicaDeAuth(HttpRequestMessage request)
    {
        var ruta = request.RequestUri?.AbsolutePath ?? string.Empty;
        return RutasPublicasAuth.Any(
            publica => ruta.EndsWith(publica, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CerrarSesionVencidaAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();
        navigation.NavigateTo("/login?sesion=vencida", forceLoad: false, replace: true);
    }
}
