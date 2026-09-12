namespace ClinicaPro.Client.Shared;

/// <summary>
/// Catálogo interno para clasificar fallos del cliente. Los códigos se conservan
/// para diagnóstico, pero nunca se incluyen en los mensajes visibles al usuario.
/// </summary>
public static class FrontendErrorCatalog
{
    public const string Connection = "CP-FE-001";
    public const string Timeout = "CP-FE-002";
    public const string InvalidResponse = "CP-FE-010";
    public const string ServerError = "CP-FE-050";
    public const string UnexpectedUi = "CP-FE-090";

    public static string WithCode(string message, string code)
        => message;

    public static string Description(string code) => code switch
    {
        Connection => "No pudimos conectarnos. Revisa tu conexión e intenta nuevamente.",
        Timeout => "La operación tardó demasiado. Intenta nuevamente.",
        InvalidResponse => "No pudimos procesar la información recibida. Intenta nuevamente.",
        ServerError => "Ocurrió un problema inesperado. Intenta nuevamente.",
        UnexpectedUi => "No pudimos mostrar esta sección. Vuelve al inicio e intenta nuevamente.",
        _ => "Ocurrió un problema inesperado."
    };
}
