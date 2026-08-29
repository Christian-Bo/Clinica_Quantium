using ClinicaPro.Domain;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Admin;

public static class RolesStaffAdministrables
{
    public static readonly HashSet<string> Nombres = new(StringComparer.OrdinalIgnoreCase)
    {
        RolNombres.Administrador,
        RolNombres.Secretaria
    };

    public static string NormalizarUno(string? rol)
    {
        var lista = Normalizar(rol is null ? [] : [rol]);
        if (lista.Count != 1)
        {
            throw new DomainException("Indique un rol: Administrador o Secretaria.");
        }

        return lista[0];
    }

    public static IReadOnlyList<string> Normalizar(IEnumerable<string>? roles)
    {
        var canonico = new List<string>();
        foreach (var crudo in roles ?? [])
        {
            var valor = (crudo ?? string.Empty).Trim();
            if (valor.Length == 0)
            {
                continue;
            }

            if (valor.Equals(RolNombres.Medico, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("El rol Médico se crea con POST /api/admin/medicos.");
            }

            if (valor.Equals(RolNombres.Paciente, StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException("El rol Paciente se obtiene al registrarse, no desde admin.");
            }

            if (!Nombres.Contains(valor))
            {
                throw new DomainException($"El rol '{valor}' no se puede asignar. Use Administrador o Secretaria.");
            }

            var nombre = valor.Equals(RolNombres.Administrador, StringComparison.OrdinalIgnoreCase)
                ? RolNombres.Administrador
                : RolNombres.Secretaria;

            if (!canonico.Contains(nombre, StringComparer.Ordinal))
            {
                canonico.Add(nombre);
            }
        }

        return canonico;
    }
}
