using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
        using var respuesta = await EnviarGetConReintentosAsync(url, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            throw await CrearExcepcionAsync(respuesta, mensajeError, ct);
        }

        T? valor;
        try
        {
            valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        }
        catch (JsonException)
        {
            throw new ApiClientException(HttpStatusCode.OK,
                FrontendErrorCatalog.WithCode("La respuesta del servidor no coincide con el formato esperado.", FrontendErrorCatalog.InvalidResponse));
        }
        return valor ?? throw new ApiClientException(
            HttpStatusCode.OK,
            FrontendErrorCatalog.WithCode(
                "El servidor respondió correctamente, pero no devolvió datos válidos.",
                FrontendErrorCatalog.InvalidResponse));
    }

    public async Task<IReadOnlyList<T>> ObtenerListaAsync<T>(
        string url,
        string mensajeError,
        CancellationToken ct = default,
        bool notFoundComoVacio = false)
    {
        using var respuesta = await EnviarGetConReintentosAsync(url, ct);

        if (notFoundComoVacio && respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            throw await CrearExcepcionAsync(respuesta, mensajeError, ct);
        }

        try
        {
            return await respuesta.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct) ?? [];
        }
        catch (JsonException)
        {
            throw new ApiClientException(HttpStatusCode.OK,
                FrontendErrorCatalog.WithCode("La lista recibida no coincide con el formato esperado.", FrontendErrorCatalog.InvalidResponse));
        }
    }

    public async Task<ResultadoOperacion<T>> ObtenerResultadoAsync<T>(
        string url,
        string mensajeError,
        CancellationToken ct = default)
    {
        try
        {
            using var respuesta = await EnviarGetConReintentosAsync(url, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                return ResultadoOperacion<T>.Fallo(
                    await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct),
                    respuesta.StatusCode);
            }

            var valor = await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return valor is null
                ? ResultadoOperacion<T>.Fallo(FrontendErrorCatalog.WithCode(
                    "El servidor no devolvió datos válidos.", FrontendErrorCatalog.InvalidResponse))
                : ResultadoOperacion<T>.Ok(valor);
        }
        catch (JsonException)
        {
            return ResultadoOperacion<T>.Fallo(FrontendErrorCatalog.WithCode(
                "La respuesta del servidor no coincide con el formato esperado.", FrontendErrorCatalog.InvalidResponse));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<T>.Fallo(MensajeConexion(ex, mensajeError));
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
                ? ResultadoOperacion<T>.Fallo(FrontendErrorCatalog.WithCode(
                    "La operación se completó, pero no se pudo leer la respuesta.", FrontendErrorCatalog.InvalidResponse))
                : ResultadoOperacion<T>.Ok(valor);
        }
        catch (JsonException)
        {
            return ResultadoOperacion<T>.Fallo(FrontendErrorCatalog.WithCode(
                "La respuesta de la operación no coincide con el formato esperado.", FrontendErrorCatalog.InvalidResponse));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ResultadoOperacion<T>.Fallo(MensajeConexion(ex, mensajeError));
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
            return ResultadoOperacion<bool>.Fallo(MensajeConexion(ex, mensajeError));
        }
    }

    /// <summary>
    /// Los GET son idempotentes, por lo que pueden recuperarse automáticamente de
    /// cortes breves, 429 y errores de gateway/servicio. Un 500 no se reintenta
    /// de inmediato: suele indicar un fallo determinista del servidor y se deja al
    /// refresco silencioso de la pantalla para no saturar el endpoint. Las operaciones de escritura nunca se
    /// reintentan aquí para evitar duplicar confirmaciones, citas o cambios.
    /// </summary>
    private async Task<HttpResponseMessage> EnviarGetConReintentosAsync(string url, CancellationToken ct)
    {
        const int maxIntentos = 3;

        for (var intento = 1; intento <= maxIntentos; intento++)
        {
            try
            {
                using var mensaje = new HttpRequestMessage(HttpMethod.Get, url);
                var respuesta = await EnviarAsync(mensaje, ct);

                if (!EsErrorTransitorio(respuesta.StatusCode) || intento == maxIntentos)
                {
                    return respuesta;
                }

                respuesta.Dispose();
            }
            catch (HttpRequestException) when (intento < maxIntentos && !ct.IsCancellationRequested)
            {
                // El siguiente intento reconstruye el HttpRequestMessage.
            }
            catch (TaskCanceledException) when (intento < maxIntentos && !ct.IsCancellationRequested)
            {
                // Timeout del HttpClient: también puede recuperarse en una lectura.
            }

            var espera = intento switch
            {
                1 => TimeSpan.FromMilliseconds(350),
                _ => TimeSpan.FromMilliseconds(900)
            };
            await Task.Delay(espera, ct);
        }

        throw new HttpRequestException("No se pudo contactar al servidor después de varios intentos.");
    }

    private static bool EsErrorTransitorio(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;

    private Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage mensaje, CancellationToken ct)
        => http.SendAsync(mensaje, HttpCompletionOption.ResponseHeadersRead, ct);

    private static async Task<ApiClientException> CrearExcepcionAsync(
        HttpResponseMessage respuesta,
        string mensajeError,
        CancellationToken ct)
        => new(
            respuesta.StatusCode,
            await ApiErrorReader.LeerAsync(respuesta, mensajeError, ct));

    private static string MensajeConexion(Exception ex, string contexto)
        => ex is TaskCanceledException
            ? FrontendErrorCatalog.WithCode(
                $"{contexto} La solicitud tardó demasiado. Intenta nuevamente.",
                FrontendErrorCatalog.Timeout)
            : FrontendErrorCatalog.WithCode(
                $"{contexto} No se pudo contactar al servidor. Verifica tu conexión y vuelve a intentar.",
                FrontendErrorCatalog.Connection);
}

public sealed class ApiClientException(HttpStatusCode statusCode, string message) : HttpRequestException(message)
{
    public HttpStatusCode CodigoEstado { get; } = statusCode;
}
