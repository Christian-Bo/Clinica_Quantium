namespace ClinicaPro.Client.Shared;

/// <summary>
/// Envuelve el resultado de una llamada a la API para que los componentes
/// no tengan que repetir lógica de try/catch ni de lectura de errores.
/// </summary>
public sealed class ResultadoOperacion<T>
{
    public bool Exito { get; }
    public T? Valor { get; }
    public string? Error { get; }

    private ResultadoOperacion(bool exito, T? valor, string? error)
    {
        Exito = exito;
        Valor = valor;
        Error = error;
    }

    public static ResultadoOperacion<T> Ok(T valor) => new(true, valor, null);

    public static ResultadoOperacion<T> Fallo(string error) => new(false, default, error);
}
