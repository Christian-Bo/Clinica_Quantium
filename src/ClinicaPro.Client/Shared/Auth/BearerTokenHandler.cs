using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Adjunta el token a cada petición y, sobre todo, reacciona cuando la API
/// responde 401 con una sesión que creíamos válida: eso significa que el token
/// venció o fue revocado.
///
/// Sin esto, el vencimiento se veía como pantallas vacías —"No tienes citas"—
/// porque los servicios capturaban el error y devolvían listas vacías. El
/// paciente veía una mentira en vez de un aviso.
/// </summary>
public sealed class BearerTokenHandler(
    TokenStorageService tokenStorage,
    ApiAuthenticationStateProvider authStateProvider,
    NavigationManager navigation) : DelegatingHandler
{
    /// <summary>
    /// El login devuelve 401 cuando las credenciales son incorrectas. Ese 401
    /// es una respuesta normal del formulario, no una sesión vencida: si lo
    /// tratáramos igual, recargaríamos la página y el usuario perdería el
    /// mensaje de "correo o contraseña incorrectos".
    /// </summary>
    private static readonly string[] RutasQueDevuelven401SinSesion =
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
        var sesion = await tokenStorage.ObtenerAsync();
        if (sesion is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        }

        var respuesta = await base.SendAsync(request, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized
            && sesion is not null
            && !EsRutaPublicaDeAuth(request))
        {
            await CerrarSesionVencidaAsync();
        }

        return respuesta;
    }

    private static bool EsRutaPublicaDeAuth(HttpRequestMessage request)
    {
        var ruta = request.RequestUri?.AbsolutePath ?? string.Empty;
        return RutasQueDevuelven401SinSesion.Any(
            publica => ruta.EndsWith(publica, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CerrarSesionVencidaAsync()
    {
        await tokenStorage.LimpiarAsync();
        authStateProvider.NotificarSesionCerrada();

        // forceLoad: false conserva la app cargada; el login lee el parámetro
        // para explicar por qué se cerró la sesión.
        navigation.NavigateTo("/login?sesion=vencida", forceLoad: false, replace: true);
    }
}
