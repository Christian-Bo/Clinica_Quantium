namespace ClinicaPro.Client.Shared;

/// <summary>
/// Catálogo estable de referencias técnicas del frontend. El usuario recibe
/// un mensaje comprensible y, solo en fallos técnicos, una referencia que el
/// equipo puede buscar sin exponer stack traces ni detalles internos.
/// </summary>
public static class FrontendErrorCatalog
{
    public const string Connection = "CP-FE-001";
    public const string Timeout = "CP-FE-002";
    public const string InvalidResponse = "CP-FE-010";
    public const string ServerError = "CP-FE-050";
    public const string UnexpectedUi = "CP-FE-090";

    public static string WithCode(string message, string code)
        => $"{message} Código de referencia: {code}.";

    public static string Description(string code) => code switch
    {
        Connection => "No fue posible establecer comunicación con la API.",
        Timeout => "La API no respondió dentro del tiempo configurado.",
        InvalidResponse => "La respuesta recibida no pudo interpretarse con el contrato esperado.",
        ServerError => "La API devolvió un error interno o de infraestructura (5xx).",
        UnexpectedUi => "Se produjo una excepción no controlada al renderizar o ejecutar la interfaz.",
        _ => "Referencia no registrada."
    };
}
