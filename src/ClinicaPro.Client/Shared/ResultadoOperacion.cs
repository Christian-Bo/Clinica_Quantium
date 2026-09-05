using System.Net;

namespace ClinicaPro.Client.Shared;

/// <summary>
/// Resultado explícito de una operación remota. Además del mensaje conserva
/// el estado HTTP cuando existe, para que la UI pueda distinguir 401, 403,
/// 409, 423 o 429 sin interpretar textos.
/// </summary>
public sealed class ResultadoOperacion<T>
{
    public bool Exito { get; }
    public T? Valor { get; }
    public string? Error { get; }
    public HttpStatusCode? StatusCode { get; }

    public bool EsNoAutorizado => StatusCode == HttpStatusCode.Unauthorized;
    public bool EsProhibido => StatusCode == HttpStatusCode.Forbidden;
    public bool EsConflicto => StatusCode == HttpStatusCode.Conflict;

    private ResultadoOperacion(bool exito, T? valor, string? error, HttpStatusCode? statusCode)
    {
        Exito = exito;
        Valor = valor;
        Error = error;
        StatusCode = statusCode;
    }

    public static ResultadoOperacion<T> Ok(T valor) => new(true, valor, null, null);

    public static ResultadoOperacion<T> Fallo(string error, HttpStatusCode? statusCode = null)
        => new(false, default, error, statusCode);
}
