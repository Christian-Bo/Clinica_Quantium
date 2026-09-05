using System.Text.Json;
using Microsoft.JSInterop;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Única fuente de verdad del JWT en el navegador. Mantiene una caché en memoria
/// y usa sessionStorage por defecto; localStorage solo si el usuario marca
/// explícitamente “Mantener sesión iniciada”. No almacena datos clínicos.
/// </summary>
public sealed class TokenStorageService(IJSRuntime jsRuntime)
{
    private const string Clave = "clinicapro.sesion";
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private AuthResponse? sesionEnCache;
    private bool yaCargada;
    private bool persistenteActual;

    public AuthResponse? Actual => sesionEnCache;
    public bool EsPersistente => persistenteActual;

    public async Task<AuthResponse?> ObtenerAsync()
    {
        if (yaCargada)
        {
            return sesionEnCache;
        }

        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("clinicaProStorage.get", Clave);
            persistenteActual = await jsRuntime.InvokeAsync<bool>("clinicaProStorage.isPersistent", Clave);
            sesionEnCache = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<AuthResponse>(json, OpcionesJson);
        }
        catch
        {
            sesionEnCache = null;
            persistenteActual = false;
        }

        if (sesionEnCache is not null && sesionEnCache.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await LimpiarAsync();
        }

        yaCargada = true;
        return sesionEnCache;
    }

    public async Task GuardarAsync(AuthResponse sesion, bool persistente = false)
    {
        sesionEnCache = sesion;
        yaCargada = true;
        persistenteActual = persistente;
        var json = JsonSerializer.Serialize(sesion, OpcionesJson);
        await jsRuntime.InvokeVoidAsync("clinicaProStorage.set", Clave, json, persistente);
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
        sesionEnCache = null;
        yaCargada = true;
        persistenteActual = false;
        await jsRuntime.InvokeVoidAsync("clinicaProStorage.remove", Clave);
    }
}
