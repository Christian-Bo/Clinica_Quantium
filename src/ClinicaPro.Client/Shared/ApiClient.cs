using System.Net;
using System.Net.Http.Json;

namespace ClinicaPro.Client.Shared;

/// <summary>
/// Punto único para llamadas HTTP del frontend. Mantiene fuera de los componentes
/// la lectura de JSON, el tratamiento de errores y la distinción entre una lista
/// vacía real y una consulta que falló.
/// </summary>
public sealed class ApiClient(HttpClient http)
{
    public async Task<T> ObtenerRequeridoAsync<T>(
        string url,
        string mensajeError,
        CancellationToken ct = default)
    {
        using var mensaje = new HttpRequestMessage(HttpMethod.Get, url);
        using var respuesta = await EnviarAsync(mensaje, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw await CrearExcepcionAsync(respuesta, mensajeError, ct);
        }

        var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return valor ?? throw new ApiClientException(
            HttpStatusCode.OK,
            "El servidor respondió correctamente, pero no devolvió datos válidos.");
    }

    public async Task<IReadOnlyList<T>> ObtenerListaAsync<T>(
        string url,
        string mensajeError,
        CancellationToken ct = default,
        bool notFoundComoVacio = false)
    {
        using var mensaje = new HttpRequestMessage(HttpMethod.Get, url);
        using var respuesta = await EnviarAsync(mensaje, ct);

        if (notFoundComoVacio && respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw await CrearExcepcionAsync(respuesta, mensajeError, ct);
        }

        return await respuesta.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct) ?? [];
    }

    public async Task<ResultadoOperacion<T>> ObtenerResultadoAsync<T>(
        string url,
        string mensajeError,
        CancellationToken ct = default)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(HttpMethod.Get, url);
            using var respuesta = await EnviarAsync(mensaje, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<T>.Fallo(
                    await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct),
                    respuesta.StatusCode);
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return valor is null
                ? ResultadoOperacion<T>.Fallo("El servidor no devolvió datos válidos.")
                : ResultadoOperacion<T>.Ok(valor);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<T>.Fallo(MensajeConexion(ex));
        }
    }

    public async Task<ResultadoOperacion<T>> EnviarAsync<T>(
        HttpMethod method,
        string url,
        object? body,
        string mensajeError,
        CancellationToken ct = default)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(method, url);
            if (body is not null)
            {
                mensaje.Content = JsonContent.Create(body);
            }

            using var respuesta = await EnviarAsync(mensaje, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<T>.Fallo(
                    await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct),
                    respuesta.StatusCode);
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return valor is null
                ? ResultadoOperacion<T>.Fallo("La operación se completó, pero no se pudo leer la respuesta.")
                : ResultadoOperacion<T>.Ok(valor);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<T>.Fallo(MensajeConexion(ex));
        }
    }

    public async Task<ResultadoOperacion<bool>> EnviarSinContenidoAsync(
        HttpMethod method,
        string url,
        object? body,
        string mensajeError,
        CancellationToken ct = default)
    {
        try
        {
            using var mensaje = new HttpRequestMessage(method, url);
            if (body is not null)
            {
                mensaje.Content = JsonContent.Create(body);
            }

            using var respuesta = await EnviarAsync(mensaje, ct);
            return respuesta.IsSuccessStatusCode
                ? ResultadoOperacion<bool>.Ok(true)
                : ResultadoOperacion<bool>.Fallo(
                    await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct),
                    respuesta.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<bool>.Fallo(MensajeConexion(ex));
        }
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage mensaje, CancellationToken ct)
    {
        try
        {
            return await http.SendAsync(mensaje, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new HttpRequestException("La solicitud tardó demasiado y fue cancelada.");
        }
    }

    private static async Task<ApiClientException> CrearExcepcionAsync(
        HttpResponseMessage respuesta,
        string mensajeError,
        CancellationToken ct)
        => new(
            respuesta.StatusCode,
            await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct));

    private static string MensajeConexion(Exception ex)
        => ex is TaskCanceledException
            ? "La solicitud tardó demasiado. Verifica tu conexión y vuelve a intentar."
            : "No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.";
}

public sealed class ApiClientException(HttpStatusCode statusCode, string message) : HttpRequestException(message)
{
    public HttpStatusCode CodigoEstado { get; } = statusCode;
}
