namespace ClinicaPro.Domain;

public static class CitaEstados
{
    public const string Solicitada = "Solicitada";
    public const string Programada = "Programada";
    public const string Confirmada = "Confirmada";
    public const string EnEspera = "En Espera";
    public const string EnAtencion = "En Atencion";
    public const string Atendida = "Atendida";
    public const string Cancelada = "Cancelada";
    public const string NoPresentada = "No presentada";
    public const string Rechazada = "Rechazada";

    public static readonly string[] Todos =
    [
        Solicitada,
        Programada,
        Confirmada,
        EnEspera,
        EnAtencion,
        Atendida,
        Cancelada,
        NoPresentada,
        Rechazada
    ];

    public static bool BloqueaHorario(string estado) =>
        estado is Solicitada or Programada or Confirmada or EnEspera or EnAtencion;

    public static bool EsConocido(string estado)
    {
        for (var i = 0; i < Todos.Length; i++)
        {
            if (string.Equals(Todos[i], estado, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
