namespace ClinicaPro.Client.Shared.Constants;

/// <summary>
/// Debe coincidir exactamente con ClinicaPro.Domain.CitaEstados (backend).
/// Se duplica aquí a propósito: Client solo puede depender de Contracts,
/// nunca de Domain (ver regla de dependencias del documento de arquitectura).
/// </summary>
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
}
