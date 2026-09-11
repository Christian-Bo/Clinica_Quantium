using ClinicaPro.Domain;

namespace ClinicaPro.Application.Admin;

/// <summary>
/// Matriz COR-12: roles de acceso acumulables y un solo tipo clínico.
/// </summary>
public static class MatrizCompatibilidadRoles
{
    public static bool PermiteRolesAccesoSimultaneos => true;

    public static IReadOnlyList<string> RolesAcceso { get; } =
    [
        RolNombres.Administrador,
        RolNombres.Secretaria
    ];

    public static IReadOnlyList<string> TiposClinicos { get; } =
    [
        TipoClinicoUsuario.Ninguno,
        TipoClinicoUsuario.Medico,
        TipoClinicoUsuario.Paciente
    ];

    public static void ExigirCompatible(IEnumerable<string> rolesAcceso, string tipoClinico)
    {
        var staff = RolesStaffAdministrables.Normalizar(rolesAcceso);
        var tipo = TipoClinicoUsuario.Normalizar(tipoClinico);

        var combinados = new List<string>(staff);
        if (tipo is TipoClinicoUsuario.Medico or TipoClinicoUsuario.Paciente)
        {
            combinados.Add(tipo);
        }

        TipoClinicoUsuario.Inferir(combinados);
    }
}
