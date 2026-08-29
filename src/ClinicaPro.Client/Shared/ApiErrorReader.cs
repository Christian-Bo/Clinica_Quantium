using System.Text.Json;

namespace ClinicaPro.Client.Shared;

/// <summary>
/// Lee el cuerpo de una respuesta de error de la Api, que puede venir en dos formas distintas:
/// - ErrorResponse { "error": "..." } cuando falla una regla de negocio (DomainException).
/// - ValidationProblemDetails { "errors": { "Campo": ["mensaje"] } } cuando falla la validación
///   automática de ASP.NET Core sobre el modelo de la petición, antes de llegar al controlador.
/// Sin esto, un 400 de validación automática se veía como un mensaje genérico sin pistas.
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
            return mensajePorDefecto;
        }

        if (string.IsNullOrWhiteSpace(cuerpo))
        {
            return mensajePorDefecto;
        }

        try
        {
            using var documento = JsonDocument.Parse(cuerpo);
            var raiz = documento.RootElement;

            if (raiz.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
            {
                var texto = errorProp.GetString();
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    return texto;
                }
            }

            if (raiz.TryGetProperty("errors", out var erroresProp) && erroresProp.ValueKind == JsonValueKind.Object)
            {
                var mensajes = new List<string>();
                foreach (var campo in erroresProp.EnumerateObject())
                {
                    if (campo.Value.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in campo.Value.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        var texto = item.GetString();
                        if (!string.IsNullOrWhiteSpace(texto))
                        {
                            mensajes.Add(texto);
                        }
                    }
                }

                if (mensajes.Count > 0)
                {
                    return string.Join(" ", mensajes);
                }
            }

            if (raiz.TryGetProperty("title", out var tituloProp) && tituloProp.ValueKind == JsonValueKind.String)
            {
                var texto = tituloProp.GetString();
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    return texto;
                }
            }
        }
        catch (JsonException)
        {
            // El cuerpo no era JSON válido; se usa el mensaje por defecto.
        }

        return mensajePorDefecto;
    }
}
