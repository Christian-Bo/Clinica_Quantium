using System.Net;
using System.Text.Json;

namespace ClinicaPro.Client.Shared;

/// <summary>
/// Traduce errores HTTP de la API a mensajes útiles sin exponer detalles técnicos.
/// Reconoce ErrorResponse y ValidationProblemDetails de ASP.NET Core.
/// </summary>
public static class ApiErrorReader
{
    public static async Task<string> LeerAsync(
        HttpResponseMessage respuesta,
        string mensajePorDefecto,
        CancellationToken ct = default)
    {
        string cuerpo;
        try
        {
            cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return MensajePorEstado(respuesta.StatusCode, mensajePorDefecto);
        }

        if (!string.IsNullOrWhiteSpace(cuerpo))
        {
            try
            {
                using var documento = JsonDocument.Parse(cuerpo);
                var raiz = documento.RootElement;

                if (raiz.TryGetProperty("error", out var errorProp)
                    && errorProp.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(errorProp.GetString()))
                {
                    return errorProp.GetString()!;
                }

                if (raiz.TryGetProperty("errors", out var erroresProp)
                    && erroresProp.ValueKind == JsonValueKind.Object)
                {
                    var mensajes = new List<string>();
                    foreach (var propiedad in erroresProp.EnumerateObject())
                    {
                        if (propiedad.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var item in propiedad.Value.EnumerateArray())
                        {
                            var texto = item.GetString();
                            if (!string.IsNullOrWhiteSpace(texto))
                            {
                                mensajes.Add(texto);
                            }
                        }
                    }

                    if (mensajes.Count > 0)
                    {
                        return string.Join(" ", mensajes.Distinct());
                    }
                }

                if (raiz.TryGetProperty("title", out var titleProp)
                    && titleProp.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(titleProp.GetString()))
                {
                    return titleProp.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Nunca mostramos HTML ni cuerpos arbitrarios devueltos por un proxy.
            }
        }

        return MensajePorEstado(respuesta.StatusCode, mensajePorDefecto);
    }

    public static string MensajePorEstado(HttpStatusCode statusCode, string mensajePorDefecto)
        => statusCode switch
        {
            HttpStatusCode.BadRequest => "Revisa la información ingresada e intenta nuevamente.",
            HttpStatusCode.Unauthorized => "Tu sesión no es válida o las credenciales son incorrectas.",
            HttpStatusCode.Forbidden => "Tu usuario no tiene permiso para realizar esta acción.",
            HttpStatusCode.NotFound => "La información solicitada ya no está disponible.",
            HttpStatusCode.Conflict => "La información cambió mientras trabajabas. Actualiza los datos e intenta nuevamente.",
            (HttpStatusCode)422 => "Hay datos que no cumplen las reglas requeridas.",
            (HttpStatusCode)423 => "La cuenta está bloqueada temporalmente por seguridad.",
            (HttpStatusCode)429 => "Se realizaron demasiados intentos. Espera unos minutos y vuelve a intentar.",
            HttpStatusCode.InternalServerError or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable
                => "El servidor no pudo completar la operación. Intenta nuevamente en unos momentos.",
            _ => mensajePorDefecto
        };
}
