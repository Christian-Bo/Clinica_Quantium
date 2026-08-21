using System.Reflection;

namespace ClinicaPro.Domain;

/// <summary>
/// Punto estable para localizar el ensamblado de Domain desde pruebas o configuración futura.
/// </summary>
public static class DomainAssembly
{
    public static Assembly Reference => typeof(DomainAssembly).Assembly;
}
