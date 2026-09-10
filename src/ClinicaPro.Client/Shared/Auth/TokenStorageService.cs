using System.Text.Json;
using Microsoft.JSInterop;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Fuente única de verdad de la sesión JWT en el navegador.
/// La sesión vive en memoria y se replica en sessionStorage por defecto;
/// localStorage se usa únicamente cuando el usuario marca "Mantener sesión iniciada".
/// </summary>
public sealed class TokenStorageService(IJSRuntime jsRuntime)
{
    private const string Clave = "clinicapro.sesion";
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim sincronizacion = new(1, 1);

    private AuthResponse? sesionEnCache;
    private bool yaCargada;
    private bool persistenteActual;

    public AuthResponse? Actual => sesionEnCache;
    public bool EsPersistente => persistenteActual;

    public async Task<AuthResponse?> ObtenerAsync()
    {
        if (yaCargada)
        {
            return SesionVigenteOSinSesion(sesionEnCache);
        }

        return await CargarDesdeNavegadorAsync(forzar: false);
    }

    /// <summary>
    /// Fuerza una lectura del almacenamiento del navegador. Se usa como respaldo
    /// por el DelegatingHandler para no depender de la caché de una instancia que
    /// pudiera haberse creado antes del login por IHttpClientFactory.
    /// </summary>
    public Task<AuthResponse?> ObtenerFrescoAsync()
        => CargarDesdeNavegadorAsync(forzar: true);

    public async Task GuardarAsync(AuthResponse sesion, bool persistente = false)
    {
        await sincronizacion.WaitAsync();
        try
        {
            // Actualizamos memoria ANTES del JS interop. Así una restricción del
            // navegador no deja al usuario autenticado sin token durante esta SPA.
            sesionEnCache = sesion;
            yaCargada = true;
            persistenteActual = persistente;

            var json = JsonSerializer.Serialize(sesion, OpcionesJson);
            try
            {
                await jsRuntime.InvokeVoidAsync("clinicaProStorage.set", Clave, json, persistente);
            }
            catch (JSException)
            {
                // La sesión sigue disponible en memoria hasta recargar la página.
            }
        }
        finally
        {
            sincronizacion.Release();
        }
    }

    /// <summary>
    /// Sin cambiar el JWT, sincroniza roles y banderas que /api/auth/me haya
    /// actualizado en servidor. Conserva la elección de persistencia original.
    /// </summary>
    public async Task<AuthResponse?> ActualizarDesdeServidorAsync(UsuarioActualDto usuario)
    {
        var sesion = await ObtenerAsync();
        if (sesion is null)
        {
            return null;
        }

        var actualizada = sesion with
        {
            UsuarioId = usuario.UsuarioId,
            Email = usuario.Email,
            Roles = usuario.Roles,
            MustChangePassword = usuario.MustChangePassword,
            PacienteId = usuario.PacienteId
        };

        await GuardarAsync(actualizada, persistenteActual);
        return actualizada;
    }

    public async Task LimpiarAsync()
    {
        await sincronizacion.WaitAsync();
        try
        {
            sesionEnCache = null;
            yaCargada = true;
            persistenteActual = false;

            try
            {
                await jsRuntime.InvokeVoidAsync("clinicaProStorage.remove", Clave);
            }
            catch (JSException)
            {
                // La memoria ya quedó limpia; no bloqueamos el logout por JS interop.
            }
        }
        finally
        {
            sincronizacion.Release();
        }
    }

    private async Task<AuthResponse?> CargarDesdeNavegadorAsync(bool forzar)
    {
        await sincronizacion.WaitAsync();
        try
        {
            if (!forzar && yaCargada)
            {
                return SesionVigenteOSinSesion(sesionEnCache);
            }

            try
            {
                var json = await jsRuntime.InvokeAsync<string?>("clinicaProStorage.get", Clave);
                persistenteActual = await jsRuntime.InvokeAsync<bool>("clinicaProStorage.isPersistent", Clave);
                sesionEnCache = string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<AuthResponse>(json, OpcionesJson);
            }
            catch (JSException)
            {
                // Si ya teníamos sesión en memoria no la destruimos por una falla
                // puntual del almacenamiento del navegador.
            }
            catch (JsonException)
            {
                sesionEnCache = null;
                persistenteActual = false;
            }

            yaCargada = true;

            if (sesionEnCache is not null && sesionEnCache.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                sesionEnCache = null;
                persistenteActual = false;
                try
                {
                    await jsRuntime.InvokeVoidAsync("clinicaProStorage.remove", Clave);
                }
                catch (JSException)
                {
                }
            }

            return sesionEnCache;
        }
        finally
        {
            sincronizacion.Release();
        }
    }

    private static AuthResponse? SesionVigenteOSinSesion(AuthResponse? sesion)
        => sesion is not null && sesion.ExpiresAtUtc > DateTimeOffset.UtcNow
            ? sesion
            : null;
}
