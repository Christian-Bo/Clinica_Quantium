using System.Text.Json;
using Microsoft.JSInterop;

namespace ClinicaPro.Client.Shared.Auth;

/// <summary>
/// Única fuente de verdad de la sesión actual en el cliente.
/// Cachea en memoria para evitar llamadas repetidas a JS interop,
/// y persiste en localStorage para sobrevivir a recargas de página.
/// </summary>
public sealed class TokenStorageService(IJSRuntime jsRuntime)
{
    private const string Clave = "clinicapro.sesion";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private AuthResponse? _sesionEnCache;
    private bool _yaCargada;

    public AuthResponse? Actual => _sesionEnCache;

    public async Task<AuthResponse?> ObtenerAsync()
    {
        if (_yaCargada)
        {
            return _sesionEnCache;
        }

        try
        {
            var json = await jsRuntime.InvokeAsync<string?>("clinicaProStorage.get", Clave);
            _sesionEnCache = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<AuthResponse>(json, OpcionesJson);
        }
        catch
        {
            _sesionEnCache = null;
        }

        if (_sesionEnCache is not null && _sesionEnCache.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            _sesionEnCache = null;
            await LimpiarAsync();
        }

        _yaCargada = true;
        return _sesionEnCache;
    }

    public async Task GuardarAsync(AuthResponse sesion)
    {
        _sesionEnCache = sesion;
        _yaCargada = true;
        var json = JsonSerializer.Serialize(sesion, OpcionesJson);
        await jsRuntime.InvokeVoidAsync("clinicaProStorage.set", Clave, json);
    }

    public async Task LimpiarAsync()
    {
        _sesionEnCache = null;
        _yaCargada = true;
        await jsRuntime.InvokeVoidAsync("clinicaProStorage.remove", Clave);
    }
}
