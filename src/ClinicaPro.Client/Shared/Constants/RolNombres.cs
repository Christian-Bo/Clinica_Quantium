namespace ClinicaPro.Client.Shared.Constants;

/// <summary>
/// Debe coincidir exactamente con ClinicaPro.Domain.RolNombres (backend).
/// Se duplica aquí a propósito: Client solo puede depender de Contracts,
/// nunca de Domain (ver regla de dependencias del documento de arquitectura).
/// </summary>
public static class RolNombres
{
    public const string Administrador = "Administrador";
    public const string Secretaria = "Secretaria";
    public const string Medico = "Medico";
    public const string Paciente = "Paciente";
}
