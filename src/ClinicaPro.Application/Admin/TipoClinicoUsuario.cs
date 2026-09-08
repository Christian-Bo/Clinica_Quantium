using ClinicaPro.Domain;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Admin;

public static class TipoClinicoUsuario
{
    public const string Ninguno = "Ninguno";
    public const string Medico = RolNombres.Medico;
    public const string Paciente = RolNombres.Paciente;

    public static string Inferir(IEnumerable<string> roles)
    {
        var lista = roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var esMedico = lista.Contains(RolNombres.Medico);
        var esPaciente = lista.Contains(RolNombres.Paciente);
        if (esMedico && esPaciente)
        {
            throw new DomainException("Un usuario no puede ser médico y paciente a la vez.");
        }

        if (esMedico)
        {
            return Medico;
        }

        if (esPaciente)
        {
            return Paciente;
        }

        return Ninguno;
    }

    public static string Normalizar(string? valor)
    {
        var tipo = (valor ?? string.Empty).Trim();
        if (tipo.Length == 0)
        {
            return Ninguno;
        }

        if (tipo.Equals(Ninguno, StringComparison.OrdinalIgnoreCase))
        {
            return Ninguno;
        }

        if (tipo.Equals(Medico, StringComparison.OrdinalIgnoreCase)
            || tipo.Equals("Médico", StringComparison.OrdinalIgnoreCase))
        {
            return Medico;
        }

        if (tipo.Equals(Paciente, StringComparison.OrdinalIgnoreCase))
        {
            return Paciente;
        }

        throw new DomainException("El tipo clínico debe ser Ninguno, Medico o Paciente.");
    }
}
