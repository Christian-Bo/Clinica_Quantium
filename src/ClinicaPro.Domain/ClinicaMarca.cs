namespace ClinicaPro.Domain;

/// <summary>
/// Nombre oficial de la clínica. Correos, Swagger y el remitente SMTP
/// deben usar esta constante en lugar de literales.
/// </summary>
public static class ClinicaMarca
{
    public const string Nombre = "Clínica Quantium";

    public const string RemitentePorDefecto = Nombre + " <noreply@clinica.local>";

    public static string Asunto(string detalle) => $"{Nombre} — {detalle}";

    public static string Remitente(string? configurado)
    {
        if (string.IsNullOrWhiteSpace(configurado))
        {
            return RemitentePorDefecto;
        }

        return configurado.Trim().Replace("Clínica Pro", Nombre, StringComparison.Ordinal);
    }
}
